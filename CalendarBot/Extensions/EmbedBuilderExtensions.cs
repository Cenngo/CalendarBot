namespace Discord
{
    internal static class EmbedBuilderExtensions
    {
        public static EmbedBuilder AddEmptyField(this EmbedBuilder builder, bool isInline = false) =>
            builder.AddField("\u200b", "\u200b", isInline);
    }
}
