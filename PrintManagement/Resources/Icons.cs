using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Media;
namespace PrintManagement.Resources;

public class Icons(string glyph, FontFamily? font = null)
{
    public static FontFamily SegoeFluentFont =>
        (FontFamily)Application.Current.Resources["SegoeFluentFont"];

    public static FontFamily ExtendedSegoeFluentFont_Regular =>
        (FontFamily)Application.Current.Resources["ExtendedSegoeFluentFont_Regular"];

    public static FontFamily ExtendedSegoeFluentFont_Filled =>
    (FontFamily)Application.Current.Resources["ExtendedSegoeFluentFont_Filled"];

    public string Glyph { get; } = glyph;
    public FontFamily Font { get; } = font ?? SegoeFluentFont;


    // --- Title ---
    public static readonly Icons Search = new("\uE721");


    // --- Navigation ---
    public static readonly Icons Filter = new("\uE71C");
    public static readonly Icons NetworkPrinter = new("\uEDA5");
    public static readonly Icons Printer = new("\uE749");
    public static readonly Icons Printer_Filled = new("\uF635", ExtendedSegoeFluentFont_Filled);

    // --- Controls ---
    // - view sublist -
    public static readonly Icons Add = new("\uE710");
    public static readonly Icons ViewAll = new("\uE8A9");
    public static readonly Icons AllApps = new("\uE71D");
    public static readonly Icons AppTitle = new("\uF12B", font: ExtendedSegoeFluentFont_Regular);
    public static readonly Icons ListDetail = new("\uE065", font: ExtendedSegoeFluentFont_Regular);

    // - controls row -
    public static readonly Icons Share = new("\uE72D");
    public static readonly Icons Delete = new("\uE74D");
    public static readonly Icons Refresh = new("\uE72C");
    public static readonly Icons Up = new("\uE74A");
    public static readonly Icons Back = new("\uE72B");
    public static readonly Icons Forward = new("\uE72A");

    // --- Filters ---
    // - Tree -
    public static readonly Icons Driver = new("\U000F013D", font: ExtendedSegoeFluentFont_Regular);
    public static readonly Icons Driver_Filled = new("\U000F013C", font: ExtendedSegoeFluentFont_Filled);

    // - Context Menu -
    public static readonly Icons Edit = new("\uE70F");
    public static readonly Icons Copy = new("\uE8C8");

    // --- Table ---
    public static readonly Icons ChevronUp = new("\uE70E");
    public static readonly Icons ChevronDown = new("\uE70D");

    // --- Settings ---
    public static readonly Icons ArrowOut = new("\uE10D", font: ExtendedSegoeFluentFont_Regular);
    public static readonly Icons Color = new("\uE790");
    public static readonly Icons SpeedMedium = new("\uEC49");
    public static readonly Icons Calendar = new("\uE787");
    public static readonly Icons TextChangeCaseRegular = new("\uF7B3", font: ExtendedSegoeFluentFont_Regular);
    public static readonly Icons CodeRegular = new("\uF2F0", font: ExtendedSegoeFluentFont_Regular);

    // --- Badge ---
    public static readonly Icons ErrorBadge12 = new("\uEDAE");
    public static readonly Icons AcceptMedium = new("\uF78C");
    public static readonly Icons Help = new("\uE897");
    public static readonly Icons InternetSharing = new("\uE704");
    public static readonly Icons Lock = new("\uE72E");
}
