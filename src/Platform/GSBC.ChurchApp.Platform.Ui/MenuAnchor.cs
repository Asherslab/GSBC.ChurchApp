using System.Globalization;

namespace GSBC.ChurchApp.Platform.Ui;

public sealed record MenuAnchor(double Left, double Top, double Width, double Height)
{
    public string Style => string.Create(CultureInfo.InvariantCulture, $"left:{Left:0.#}px;top:{Top:0.#}px;width:{Width:0.#}px;height:{Height:0.#}px");
}
