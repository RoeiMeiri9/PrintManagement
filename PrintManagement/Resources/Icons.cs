using Microsoft.UI.Xaml.Media;
namespace PrintManagement.Resources
{
    /// <summary>
    /// Record of all icons used accross the system
    /// </summary>
    /// <param name="Glyph">Glyph code</param>
    /// <param name="Font">Font the glyph belongs to. When null, the font is part of the default iconFonts</param>
    public record Icons(string Glyph, FontFamily? Font = null)
    {
        // --- Title ---
        public static readonly Icons Search = new("\uE721");


        // --- Navigation ---
        public static readonly Icons Filter = new("\uE71C");
        public static readonly Icons NetworkPrinter = new("\uEDA5");
        public static readonly Icons Printer = new("\uE749");

        // --- Controls ---
        // - view sublist -
        public static readonly Icons Add = new("\uE710");
        public static readonly Icons AppTitle = new("\uF12B", Font: AppFonts.ExtendedFluentIconsFont);
        public static readonly Icons ListDetail = new("\uE065", Font: AppFonts.ExtendedFluentIconsFont);

        // - controls row -
        public static readonly Icons Export = new("\uEDE1");
        public static readonly Icons Clear = new("\uE894");
        public static readonly Icons Refresh = new("\uE72C");
        public static readonly Icons Up = new("\uE74A");
        public static readonly Icons Back = new("\uE72B");
        public static readonly Icons Forward = new("\uE72A");

        // --- Filters ---
        // - Tree -
        public static readonly Icons Driver = new("\U000F013D", Font: AppFonts.ExtendedFluentIconsFont);

        // --- Table ---
        public static readonly Icons ChevronUp = new("\uE70D");
        public static readonly Icons ChevronDown = new("\uE70E");

        // --- Settings ---
        public static readonly Icons ArrowOut = new("\uE10D", Font: AppFonts.ExtendedFluentIconsFont);


    }
}
