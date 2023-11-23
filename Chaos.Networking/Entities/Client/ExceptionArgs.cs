using Chaos.Packets.Abstractions;

namespace Chaos.Networking.Entities.Client;

public sealed record ExceptionArgs(string exceptionMsg) : IReceiveArgs;