using Chaos.DarkAges.Definitions;
using Chaos.IO.Memory;
using Chaos.Networking.Abstractions.Definitions;
using Chaos.Networking.Entities.Server;
using Chaos.Packets.Abstractions;

namespace Chaos.Networking.Converters.Server;

/// <summary>
///     Provides serialization and deserialization logic for <see cref="DisplayBoardArgs" />
/// </summary>
public sealed class DisplayBoardConverter : PacketConverterBase<DisplayBoardArgs>
{
    /// <inheritdoc />
    public override byte OpCode => (byte)ServerOpCode.DisplayBoard;

    /// <inheritdoc />
    public override DisplayBoardArgs Deserialize(ref SpanReader reader) => null;

    /// <inheritdoc />
    public override void Serialize(ref SpanWriter writer, DisplayBoardArgs args)
    {
        writer.WriteByte((byte)args.Type);

        switch (args.Type)
        {
            case BoardOrResponseType.BoardList:
            {
                writer.WriteUInt16((ushort)args.Boards!.Count);

                foreach (var board in args.Boards)
                {
                    writer.WriteUInt16(board.BoardId);
                    writer.WriteString(board.Name);
                }

                break;
            }
            case BoardOrResponseType.PublicBoard:
            {
                writer.WriteBoolean(false);
                writer.WriteUInt16(args.Board!.BoardId);
                writer.WriteString(args.Board.Name);

                //order posts newest to oldest
                var orderedPosts = (IEnumerable<PostInfo>)args.Board.Posts.OrderByDescending(p => p.PostId);

                //if there's a StartPostId, only send posts with an id greater than or equal to it
                if (args.StartPostId.HasValue)
                    orderedPosts = orderedPosts.Where(p => p.PostId <= args.StartPostId.Value);

                //only send up to 127 posts (i have no fucking clue why its sbyte.MaxValue)
                var posts = orderedPosts.Take(sbyte.MaxValue)
                                        .ToList();

                writer.WriteSByte((sbyte)posts.Count);

                foreach (var post in posts)
                {
                    writer.WriteBoolean(post.IsHighlighted);
                    writer.WriteInt16(post.PostId);
                    writer.WriteString(post.Author);
                    writer.WriteByte((byte)post.MonthOfYear);
                    writer.WriteByte((byte)post.DayOfMonth);
                    writer.WriteString(post.Subject);
                }

                break;
            }
            case BoardOrResponseType.PublicPost:
            {
                writer.WriteBoolean(args.EnablePrevBtn);
                writer.WriteByte(0); //LI: what is this for?
                writer.WriteInt16(args.Post!.PostId);
                writer.WriteString(args.Post.Author);
                writer.WriteByte((byte)args.Post.MonthOfYear);
                writer.WriteByte((byte)args.Post.DayOfMonth);
                writer.WriteString(args.Post.Subject);
                writer.WriteString(args.Post.Message);

                break;
            }
            case BoardOrResponseType.MailBoard:
            {
                writer.WriteBoolean(false);
                writer.WriteUInt16(args.Board!.BoardId);
                writer.WriteString(args.Board.Name);

                //order posts newest to oldest
                var orderedPosts = (IEnumerable<PostInfo>)args.Board.Posts.OrderByDescending(p => p.PostId);

                //if there's a StartPostId, only send posts with an id greater than or equal to it
                if (args.StartPostId.HasValue)
                    orderedPosts = orderedPosts.Where(p => p.PostId <= args.StartPostId.Value);

                //only send up to 127 posts (i have no fucking clue why its sbyte.MaxValue)
                var posts = orderedPosts.Take(sbyte.MaxValue)
                                        .ToList();

                writer.WriteSByte((sbyte)posts.Count);

                foreach (var post in posts)
                {
                    writer.WriteBoolean(post.IsHighlighted);
                    writer.WriteInt16(post.PostId);
                    writer.WriteString(post.Author);
                    writer.WriteByte((byte)post.MonthOfYear);
                    writer.WriteByte((byte)post.DayOfMonth);
                    writer.WriteString(post.Subject);
                }

                break;
            }
            case BoardOrResponseType.MailPost:
            {
                writer.WriteBoolean(args.EnablePrevBtn);
                writer.WriteByte(0); //LI: what is this for?
                writer.WriteInt16(args.Post!.PostId);
                writer.WriteString(args.Post.Author);
                writer.WriteByte((byte)args.Post.MonthOfYear);
                writer.WriteByte((byte)args.Post.DayOfMonth);
                writer.WriteString(args.Post.Subject);
                writer.WriteString(args.Post.Message);

                break;
            }
            case BoardOrResponseType.SubmitPostResponse:
            case BoardOrResponseType.DeletePostResponse:
            case BoardOrResponseType.HighlightPostResponse:
            {
                writer.WriteBoolean(args.Success!.Value);
                writer.WriteString(args.ResponseMessage!);

                break;
            }
            default:
                throw new ArgumentOutOfRangeException();
        }
    }
}