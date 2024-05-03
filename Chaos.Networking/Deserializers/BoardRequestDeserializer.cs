using Chaos.Common.Definitions;
using Chaos.IO.Memory;
using Chaos.Networking.Entities.Client;
using Chaos.Packets.Abstractions;
using Chaos.Packets.Abstractions.Definitions;

namespace Chaos.Networking.Deserializers;

/// <summary>
///     Deserializes a buffer into <see cref="BoardRequestArgs" />
/// </summary>
public sealed record BoardRequestDeserializer : ClientPacketDeserializer<BoardRequestArgs>
{
    /// <inheritdoc />
    public override ClientOpCode ClientOpCode => ClientOpCode.BoardRequest;

    /// <inheritdoc />
    public override BoardRequestArgs Deserialize(ref SpanReader reader)
    {
        var boardRequestType = (BoardRequestType)reader.ReadByte();

        switch (boardRequestType)
        {
            case BoardRequestType.BoardList:
            {
                return new BoardRequestArgs(boardRequestType);
            }
            case BoardRequestType.ViewBoard:
            {
                var boardId = reader.ReadUInt16();
                var startPostId = reader.ReadInt16();
                //var unknown = reader.ReadByte();

                return new BoardRequestArgs(boardRequestType, boardId, StartPostId: startPostId);
            }
            case BoardRequestType.ViewPost:
            {
                var boardId = reader.ReadUInt16();
                var postId = reader.ReadInt16();
                var controls = (BoardControls)reader.ReadSByte();

                return new BoardRequestArgs(
                    boardRequestType,
                    boardId,
                    postId,
                    Controls: controls);
            }
            case BoardRequestType.NewPost:
            {
                var boardId = reader.ReadUInt16();
                var subject = reader.ReadString8();
                var message = reader.ReadString16();

                return new BoardRequestArgs(
                    boardRequestType,
                    boardId,
                    Subject: subject,
                    Message: message);
            }
            case BoardRequestType.Delete:
            {
                var boardId = reader.ReadUInt16();
                var postId = reader.ReadInt16();

                return new BoardRequestArgs(boardRequestType, boardId, postId);
            }
            case BoardRequestType.SendMail:
            {
                var boardId = reader.ReadUInt16();
                var to = reader.ReadString8();
                var subject = reader.ReadString8();
                var message = reader.ReadString16();

                return new BoardRequestArgs(
                    boardRequestType,
                    boardId,
                    To: to,
                    Subject: subject,
                    Message: message);
            }
            case BoardRequestType.Highlight:
            {
                var boardId = reader.ReadUInt16();
                var postId = reader.ReadInt16();

                return new BoardRequestArgs(boardRequestType, boardId, postId);
            }
            default:
                throw new ArgumentOutOfRangeException();
        }
    }
}