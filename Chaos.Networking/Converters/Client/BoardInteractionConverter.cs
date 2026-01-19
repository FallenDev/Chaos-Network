using Chaos.DarkAges.Definitions;
using Chaos.IO.Memory;
using Chaos.Networking.Abstractions.Definitions;
using Chaos.Networking.Entities.Client;
using Chaos.Packets.Abstractions;

namespace Chaos.Networking.Converters.Client;

public sealed class BoardInteractionConverter : PacketConverterBase<BoardInteractionArgs>
{
    public override byte OpCode => (byte)ClientOpCode.BoardInteraction;

    public override BoardInteractionArgs Deserialize(ref SpanReader reader)
    {
        var boardRequestType = reader.ReadByte();

        var args = new BoardInteractionArgs
        {
            BoardRequestType = (BoardRequestType)boardRequestType
        };

        switch (args.BoardRequestType)
        {
            case BoardRequestType.BoardList:
                break;
            case BoardRequestType.ViewBoard:
            {
                var boardId = reader.ReadUInt16();
                var startPostId = reader.ReadInt16();

                //var unknown = reader.ReadByte(); //idk but it's always 240
                args.BoardId = boardId;
                args.StartPostId = startPostId;

                break;
            }
            case BoardRequestType.ViewPost:
            {
                var boardId = reader.ReadUInt16();
                var postId = reader.ReadInt16();
                var controls = (BoardControls)reader.ReadSByte();

                args.BoardId = boardId;
                args.PostId = postId;
                args.Controls = controls;

                break;
            }
            case BoardRequestType.NewPost:
            {
                var boardId = reader.ReadUInt16();
                var subject = reader.ReadString8();
                var message = reader.ReadString16();

                args.BoardId = boardId;
                args.Subject = subject;
                args.Message = message;

                break;
            }
            case BoardRequestType.Delete:
            {
                var boardId = reader.ReadUInt16();
                var postId = reader.ReadInt16();

                args.BoardId = boardId;
                args.PostId = postId;

                break;
            }
            case BoardRequestType.SendMail:
            {
                var boardId = reader.ReadUInt16();
                var to = reader.ReadString8();
                var subject = reader.ReadString8();
                var message = reader.ReadString16();

                args.BoardId = boardId;
                args.To = to;
                args.Subject = subject;
                args.Message = message;

                break;
            }
            case BoardRequestType.Highlight:
            {
                var boardId = reader.ReadUInt16();
                var postId = reader.ReadInt16();

                args.BoardId = boardId;
                args.PostId = postId;

                break;
            }
            default:
                throw new ArgumentOutOfRangeException();
        }

        return args;
    }
}