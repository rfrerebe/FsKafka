﻿// Stand-ins for stuff presented in Confluent.Kafka v1
namespace Confluent.Kafka

open System
open System.Collections.Generic

[<RequireQualifiedAccess; Struct>]
type CompressionType = None | GZip | Snappy | Lz4

[<RequireQualifiedAccess; Struct>]
type Acks = Zero | Leader | All

[<RequireQualifiedAccess; Struct>]
type Partitioner = Random | Consistent | ConsistentRandom

[<RequireQualifiedAccess; Struct>]
type AutoOffsetReset = Earliest | Latest | Error

[<RequireQualifiedAccess>]
module ConfigHelpers =

    [<NoComparison; NoEquality>]
    type ConfigKey<'T> = Key of id : string * render : ('T -> obj) with
        member k.Id = let (Key(id,_)) = k in id

        static member (==>) (Key(id, render) : ConfigKey<'T>, value : 'T) =
            match render value with
            | null -> nullArg id
            | :? string as str when String.IsNullOrWhiteSpace str -> nullArg id
            | obj -> KeyValuePair(id, obj)

    let private mkKey id render = Key(id, render >> box)

    (* shared keys applying to producers and consumers alike *)

    let bootstrapServers    = mkKey "bootstrap.servers" id<string>
    let clientId            = mkKey "client.id" id<string>
    let logConnectionClose  = mkKey "log.connection.close" id<bool>
    let maxInFlight         = mkKey "max.in.flight.requests.per.connection" id<int>
    let retryBackoff        = mkKey "retry.backoff.ms" id<int>
    let socketKeepAlive     = mkKey "socket.keepalive.enable" id<bool>
    let statisticsInterval  = mkKey "statistics.interval.ms" id<int>

    /// Config keys applying to Producers
    module Producer =
        let acks                = mkKey "acks" (function Acks.Zero -> 0 | Acks.Leader -> 1 | Acks.All -> -1)
        // NOTE CK 0.11.4 adds a "compression.type" alias - we use "compression.codec" as 0.11.3 will otherwise throw
        let compression         = mkKey "compression.codec" (function CompressionType.None -> "none" | CompressionType.GZip -> "gzip" | CompressionType.Snappy -> "snappy" | CompressionType.Lz4 -> "lz4")
        let linger              = mkKey "linger.ms" id<int>
        let messageSendRetries  = mkKey "message.send.max.retries" id<int>
        let partitioner         = mkKey "partitioner" (function Partitioner.Random -> "random" | Partitioner.Consistent -> "consistent" | Partitioner.ConsistentRandom -> "consistent_random")
        let requestTimeoutMs    = mkKey "request.timeout.ms" id<int>

     /// Config keys applying to Consumers
    module Consumer =
        let autoCommitInterval  = mkKey "auto.commit.interval.ms" id<int>
        let autoOffsetReset     = mkKey "auto.offset.reset" (function AutoOffsetReset.Earliest -> "earliest" | AutoOffsetReset.Latest -> "latest" | AutoOffsetReset.Error -> "error")
        let enableAutoCommit    = mkKey "enable.auto.commit" id<bool>
        let enableAutoOffsetStore = mkKey "enable.auto.offset.store" id<bool>
        let groupId             = mkKey "group.id" id<string>
        let fetchMaxBytes       = mkKey "fetch.message.max.bytes" id<int>
        let fetchMinBytes       = mkKey "fetch.min.bytes" id<int>

[<AutoOpen>]
module private NullableHelpers =
    let (|Null|HasValue|) (x:Nullable<'T>) =
        if x.HasValue then HasValue x.Value
        else Null

type ProducerConfig() =
    let values = Dictionary<string,obj>()
    let set key value = values.[key] <- box value

    member __.Set(key, value) = set key value

    member val ClientId = null with get, set
    member val BootstrapServers = null with get, set
    member val RetryBackoffMs = Nullable() with get, set
    member val MessageSendMaxRetries = Nullable() with get, set
    member val Acks = Nullable() with get, set
    member val SocketKeepaliveEnable = Nullable() with get, set
    member val LogConnectionClose = Nullable() with get, set
    member val MaxInFlight = Nullable() with get, set
    member val LingerMs = Nullable() with get, set
    member val Partitioner = Nullable() with get, set
    member val CompressionType = Nullable() with get, set
    member val RequestTimeoutMs = Nullable() with get, set
    member val StatisticsIntervalMs = Nullable() with get, set

    member __.Render() : KeyValuePair<string,obj>[] =
        [|  match __.ClientId               with null -> () | v ->          yield ConfigHelpers.clientId ==> v
            match __.BootstrapServers       with null -> () | v ->          yield ConfigHelpers.bootstrapServers ==> v
            match __.RetryBackoffMs         with Null -> () | HasValue v -> yield ConfigHelpers.retryBackoff ==> v
            match __.MessageSendMaxRetries  with Null -> () | HasValue v -> yield ConfigHelpers.Producer.messageSendRetries ==> v
            match __.Acks                   with Null -> () | HasValue v -> yield ConfigHelpers.Producer.acks ==> v
            match __.SocketKeepaliveEnable  with Null -> () | HasValue v -> yield ConfigHelpers.socketKeepAlive ==> v
            match __.LogConnectionClose     with Null -> () | HasValue v -> yield ConfigHelpers.logConnectionClose ==> v
            match __.MaxInFlight            with Null -> () | HasValue v -> yield ConfigHelpers.maxInFlight ==> v
            match __.LingerMs               with Null -> () | HasValue v -> yield ConfigHelpers.Producer.linger ==> v
            match __.Partitioner            with Null -> () | HasValue v -> yield ConfigHelpers.Producer.partitioner ==> v
            match __.CompressionType        with Null -> () | HasValue v -> yield ConfigHelpers.Producer.compression ==> v
            match __.RequestTimeoutMs       with Null -> () | HasValue v -> yield ConfigHelpers.Producer.requestTimeoutMs ==> v
            match __.StatisticsIntervalMs   with Null -> () | HasValue v -> yield ConfigHelpers.statisticsInterval ==> v
            yield! values |]

type ConsumerConfig() =
    let values = Dictionary<string,obj>()
    let set key value = values.[key] <- box value

    member __.Set(key, value) = set key value

    member val ClientId = null with get, set
    member val BootstrapServers = null with get, set
    member val GroupId = null with get, set
    member val AutoOffsetReset = Nullable() with get, set
    member val FetchMaxBytes = Nullable() with get, set
    member val EnableAutoCommit = Nullable() with get, set
    member val EnableAutoOffsetStore = Nullable() with get, set
    member val LogConnectionClose = Nullable() with get, set
    member val FetchMinBytes = Nullable() with get, set
    member val StatisticsIntervalMs = Nullable() with get, set
    member val AutoCommitIntervalMs = Nullable() with get, set

    member __.Render() : KeyValuePair<string,obj>[] =
        [|  match __.ClientId               with null -> () | v ->          yield ConfigHelpers.clientId ==> v
            match __.BootstrapServers       with null -> () | v ->          yield ConfigHelpers.bootstrapServers ==> v
            match __.GroupId                with null -> () | v ->          yield ConfigHelpers.Consumer.groupId ==> v
            match __.AutoOffsetReset        with Null -> () | HasValue v -> yield ConfigHelpers.Consumer.autoOffsetReset ==> v
            match __.FetchMaxBytes          with Null -> () | HasValue v -> yield ConfigHelpers.Consumer.fetchMaxBytes ==> v
            match __.LogConnectionClose     with Null -> () | HasValue v -> yield ConfigHelpers.logConnectionClose ==> v
            match __.EnableAutoCommit       with Null -> () | HasValue v -> yield ConfigHelpers.Consumer.enableAutoCommit ==> v
            match __.EnableAutoOffsetStore  with Null -> () | HasValue v -> yield ConfigHelpers.Consumer.enableAutoOffsetStore ==> v
            match __.FetchMinBytes          with Null -> () | HasValue v -> yield ConfigHelpers.Consumer.fetchMinBytes ==> v
            match __.AutoCommitIntervalMs   with Null -> () | HasValue v -> yield ConfigHelpers.Consumer.autoCommitInterval ==> v
            match __.StatisticsIntervalMs   with Null -> () | HasValue v -> yield ConfigHelpers.statisticsInterval ==> v
            yield! values |]
