using Chaos.DarkAges.Definitions;
using Chaos.IO.Memory;
using Chaos.Networking.Abstractions.Definitions;
using Chaos.Networking.Entities.Server;
using Chaos.Packets.Abstractions;

namespace Chaos.Networking.Converters.Server;

public sealed class DisplayBoardConverter : PacketConverterBase<DisplayBoardArgs>
{
    public override byte OpCode => (byte)ServerOpCode.DisplayBoard;

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
                    writer.WriteString8(board.Name);
                }

                break;
            }
            case BoardOrResponseType.PublicBoard:
            {
                writer.WriteBoolean(false);
                writer.WriteUInt16(args.Board!.BoardId);
                writer.WriteString8(args.Board.Name);

                // Order posts newest to oldest
                var orderedPosts = (IEnumerable<PostInfo>)args.Board.Posts.OrderByDescending(p => p.PostId);

                // If there's a StartPostId, only send posts with an id greater than or equal to it
                if (args.StartPostId.HasValue)
                    orderedPosts = orderedPosts.Where(p => p.PostId <= args.StartPostId.Value);

                // Only send up to 127 posts
                var posts = orderedPosts.Take(sbyte.MaxValue)
                                        .ToList();

                writer.WriteSByte((sbyte)posts.Count);

                foreach (var post in posts)
                {
                    writer.WriteBoolean(post.IsHighlighted);
                    writer.WriteInt16(post.PostId);
                    writer.WriteString8(post.Author);
                    writer.WriteByte((byte)post.MonthOfYear);
                    writer.WriteByte((byte)post.DayOfMonth);
                    writer.WriteString8(post.Subject);
                }

                break;
            }
            case BoardOrResponseType.PublicPost:
            {
                writer.WriteBoolean(args.EnablePrevBtn);
                writer.WriteByte(0);
                writer.WriteInt16(args.Post!.PostId);
                writer.WriteString8(args.Post.Author);
                writer.WriteByte((byte)args.Post.MonthOfYear);
                writer.WriteByte((byte)args.Post.DayOfMonth);
                writer.WriteString8(args.Post.Subject);
                writer.WriteString16(args.Post.Message);

                break;
            }
            case BoardOrResponseType.MailBoard:
            {
                writer.WriteBoolean(false);
                writer.WriteUInt16(args.Board!.BoardId);
                writer.WriteString8(args.Board.Name);

                // Order posts newest to oldest
                var orderedPosts = (IEnumerable<PostInfo>)args.Board.Posts.OrderByDescending(p => p.PostId);

                // If there's a StartPostId, only send posts with an id greater than or equal to it
                if (args.StartPostId.HasValue)
                    orderedPosts = orderedPosts.Where(p => p.PostId <= args.StartPostId.Value);

                // Only send up to 127 posts
                var posts = orderedPosts.Take(sbyte.MaxValue)
                                        .ToList();

                writer.WriteSByte((sbyte)posts.Count);

                foreach (var post in posts)
                {
                    writer.WriteBoolean(post.IsHighlighted);
                    writer.WriteInt16(post.PostId);
                    writer.WriteString8(post.Author);
                    writer.WriteByte((byte)post.MonthOfYear);
                    writer.WriteByte((byte)post.DayOfMonth);
                    writer.WriteString8(post.Subject);
                }

                break;
            }
            case BoardOrResponseType.MailPost:
            {
                writer.WriteBoolean(args.EnablePrevBtn);
                writer.WriteByte(0);
                writer.WriteInt16(args.Post!.PostId);
                writer.WriteString8(args.Post.Author);
                writer.WriteByte((byte)args.Post.MonthOfYear);
                writer.WriteByte((byte)args.Post.DayOfMonth);
                writer.WriteString8(args.Post.Subject);
                writer.WriteString16(args.Post.Message);

                break;
            }
            case BoardOrResponseType.SubmitPostResponse:
            case BoardOrResponseType.DeletePostResponse:
            case BoardOrResponseType.HighlightPostResponse:
            {
                writer.WriteBoolean(args.Success!.Value);
                writer.WriteString8(args.ResponseMessage!);

                break;
            }
            default:
                throw new ArgumentOutOfRangeException();
        }
    }
}