namespace GSBC.ChurchApp.Platform.Ui.Surfaces;

public enum PhoneCutout
{
    None,
    Notch,
    DynamicIsland,
}

public sealed record PhoneDevice(string Name, int Width, int Height, int TopInset, int BottomInset, PhoneCutout Cutout)
{
    public static readonly PhoneDevice IPhone15 = new("iPhone 15", 393, 852, 59, 34, PhoneCutout.DynamicIsland);
    public static readonly PhoneDevice IPhone15Plus = new("iPhone 15 Plus", 430, 932, 59, 34, PhoneCutout.DynamicIsland);
    public static readonly PhoneDevice IPhone15Pro = new("iPhone 15 Pro", 393, 852, 59, 34, PhoneCutout.DynamicIsland);
    public static readonly PhoneDevice IPhone15ProMax = new("iPhone 15 Pro Max", 430, 932, 59, 34, PhoneCutout.DynamicIsland);
    public static readonly PhoneDevice IPhone16 = new("iPhone 16", 393, 852, 59, 34, PhoneCutout.DynamicIsland);
    public static readonly PhoneDevice IPhone16Plus = new("iPhone 16 Plus", 430, 932, 59, 34, PhoneCutout.DynamicIsland);
    public static readonly PhoneDevice IPhone16Pro = new("iPhone 16 Pro", 402, 874, 62, 34, PhoneCutout.DynamicIsland);
    public static readonly PhoneDevice IPhone16ProMax = new("iPhone 16 Pro Max", 440, 956, 62, 34, PhoneCutout.DynamicIsland);
    public static readonly PhoneDevice IPhone16e = new("iPhone 16e", 390, 844, 47, 34, PhoneCutout.Notch);
    public static readonly PhoneDevice IPhone17 = new("iPhone 17", 402, 874, 62, 34, PhoneCutout.DynamicIsland);
    public static readonly PhoneDevice IPhoneAir = new("iPhone Air", 420, 912, 62, 34, PhoneCutout.DynamicIsland);
    public static readonly PhoneDevice IPhone17Pro = new("iPhone 17 Pro", 402, 874, 62, 34, PhoneCutout.DynamicIsland);
    public static readonly PhoneDevice IPhone17ProMax = new("iPhone 17 Pro Max", 440, 956, 62, 34, PhoneCutout.DynamicIsland);

    public static readonly IReadOnlyList<PhoneDevice> All =
    [
        IPhone15, IPhone15Plus, IPhone15Pro, IPhone15ProMax,
        IPhone16, IPhone16Plus, IPhone16Pro, IPhone16ProMax, IPhone16e,
        IPhone17, IPhoneAir, IPhone17Pro, IPhone17ProMax,
    ];

    public string Size => $"{Width} × {Height}";
}
