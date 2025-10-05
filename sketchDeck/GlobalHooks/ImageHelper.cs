namespace sketchDeck.GlobalHooks;

public static class ThumbnailHelper
{
    public static string CurrentSelectedView { get; set; } = "Details";

    public static int CurrentThumbSize => CurrentSelectedView switch
    {
        "Gigantic" => 0,
        "Big"      => 0,
        "Medium"   => 256,
        "Small"    => 128,
        "Details"  => 64,
        _          => 0
    };
}