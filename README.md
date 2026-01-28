# Core.Network.Dispatcher

Kind-based dispatch utilities for **Core.Network**.

This package provides a small, transport-agnostic dispatch layer for batched event streams:
it maps `KindId -> handler`, can auto-register DTO parsers via reflection, and can unpack
event batches using `EventsReader<TKind, TCodec>`.

It intentionally does **not** define any project-specific kind enums or message types.
Consumers decide what kinds exist and what to do with parsed DTOs (e.g. publish into an event bus).

- [Core.Network (base)](https://github.com/Fur-Fighters-Frenzy/Core.Network)
- [Core.Network.Envelope](https://github.com/Fur-Fighters-Frenzy/Core.Network.Envelope)

> **Status:** WIP

---

## What’s included

- `IServerDispatcher`: `ushort kind -> handler(ReadOnlySpan<byte>)`
- `IClientDispatcher`: `ushort kind -> handler(ReadOnlySpan<byte>, PlayerId sender)`
- `EventDtoReflection`: finds DTO structs implementing `IEventDto<TKind>` and their `TryFromBytes` methods
- `ServerDispatcherBootstrapper<TDispatcher, TKind>`: auto-registers DTO parsers into a server dispatcher
- `ClientDispatcherBootstrapper<TDispatcher, TKind>`: auto-registers DTO parsers into a client dispatcher
- `ServerDispatcherUnpackExtensions`: reads `EventsReader<TKind, TCodec>` from a `NetEnvelope` and dispatches each event
- `ClientDispatcherUnpackExtensions`: same, but dispatches with `PlayerId sender`

---

## DTO requirements

A DTO must:

- be a `struct`
- implement `IEventDto<TKind>`
- expose a static parser:

```csharp
public static bool TryFromBytes(ReadOnlySpan<byte> span, out MyDto dto)
````

`TKind` is expected to have `ushort` underlying type (dispatcher uses `ushort kindId`).

---

## Usage

### Auto-register all DTO parsers (server-side dispatch)

```csharp
var dispatcher = new MyServerDispatcher();

ServerDispatcherBootstrapper<MyServerDispatcher, MyServerEventKind>.Initialize(
    dispatcher,
    onParsed: (kind, dtoObj) =>
    {
        // Single routing point for parsed DTOs (project decides what to do here).
        // Example: publish into your event bus.
        BucketHub.PublishDynamic(dtoObj);
    },
    assemblies: typeof(MyServerEventKind).Assembly);
```

### Auto-register all DTO parsers (client-side dispatch)

```csharp
var dispatcher = new MyClientDispatcher();

ClientDispatcherBootstrapper<MyClientDispatcher, MyClientEventKind>.Initialize(
    dispatcher,
    onParsed: (kind, sender, dtoObj) =>
    {
        // Single routing point for parsed DTOs coming from a client.
        BucketHub.PublishDynamic(dtoObj);
    },
    assemblies: typeof(MyClientEventKind).Assembly);
```

### Unpack and dispatch an event batch (server dispatcher)

```csharp
dispatcher.Unpack<MyServerEventKind, MyServerKindCodec>(env);
```

### Unpack and dispatch an event batch (client dispatcher)

```csharp
dispatcher.Unpack<MyClientEventKind, MyClientKindCodec>(env, senderPid);
```

---

## Notes

* Reflection-based DTO discovery may require `Preserve`/`link.xml` under IL2CPP stripping.
* The bootstrappers do not allocate per event dispatch (delegates are built once at init).
* `Unpack` uses `EventsReader<TKind, TCodec>` to read `[kind + blob]` items from the envelope payload.
* This package does not impose client/server semantics; it only provides two dispatcher shapes:
  with and without sender context.

---

# Part of the Core Project

This package is part of the **Core** project, which consists of multiple Unity packages.
See the full project here: [Core](https://github.com/Fur-Fighters-Frenzy/Core)

---