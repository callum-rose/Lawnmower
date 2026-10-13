using TMPro;
using UnityEngine;

namespace BunnyHeaven.UI
{
    public enum ThemeColor
    {
        Yellow,
        YellowShadow,
        Teal,
        TealShadow,
        Green,
        GreenShadow,
        Blue,
        BlueShadow,
        Red,
        RedShadow,
        Ink,
        InkShadow,
        Brown,
        Muted,
        Cream,
        White,
        GreenPale,
        SegmentTrack,
        SliderTrack,
        Scrim,
        Doodle,
    }

    public enum ButtonColor
    {
        Green,
        Yellow,
        Blue,
        Red,
        White,
        WhiteOnTeal,
    }

    public enum ThemeFont
    {
        Heading,
        Body,
        BodyBold,
    }

    public readonly struct ButtonPalette
    {
        public readonly ThemeColor Fill;
        public readonly ThemeColor Shadow;
        public readonly ThemeColor Content;

        public ButtonPalette(ThemeColor fill, ThemeColor shadow, ThemeColor content)
        {
            Fill = fill;
            Shadow = shadow;
            Content = content;
        }
    }

    /// <summary>Colour tokens and fonts taken from the Bunny Heaven Figma file (hand-drawn style D).</summary>
    [CreateAssetMenu(menuName = "Bunny Heaven/UI Theme", fileName = "BunnyHeavenTheme")]
    public class UiTheme : ScriptableObject
    {
        [Header("Surfaces")]
        public Color yellow = Hex(0xFFC94A);
        public Color yellowShadow = Hex(0xE9A92C);
        public Color teal = Hex(0x86CFD6);
        public Color tealShadow = Hex(0x5FB1BA);
        public Color cream = Hex(0xFFF7E3);
        public Color white = Color.white;
        public Color segmentTrack = Hex(0xFBE7B5);
        public Color sliderTrack = Hex(0xF1D79A);
        public Color scrim = Hex(0x3B2A20, 0.5f);
        public Color doodle = Hex(0xE9A92C, 0.55f);

        [Header("Accents")]
        public Color green = Hex(0x5DBB4C);
        public Color greenShadow = Hex(0x3E8E35);
        public Color greenPale = Hex(0xDFF5D6);
        public Color blue = Hex(0x4E8BE6);
        public Color blueShadow = Hex(0x3466B8);
        public Color red = Hex(0xEE4D64);
        public Color redShadow = Hex(0xC2364B);

        [Header("Ink")]
        public Color ink = Hex(0x3B2A20);
        public Color inkShadow = Hex(0x2A1E17);
        public Color brown = Hex(0x9A6A1E);
        public Color muted = Hex(0x8A6A4F);

        [Header("Fonts")]
        public TMP_FontAsset headingFont;
        public Material headingShadowMaterial;
        public TMP_FontAsset bodyFont;
        public TMP_FontAsset bodyBoldFont;

        public Color Get(ThemeColor token) => token switch
        {
            ThemeColor.Yellow => yellow,
            ThemeColor.YellowShadow => yellowShadow,
            ThemeColor.Teal => teal,
            ThemeColor.TealShadow => tealShadow,
            ThemeColor.Green => green,
            ThemeColor.GreenShadow => greenShadow,
            ThemeColor.Blue => blue,
            ThemeColor.BlueShadow => blueShadow,
            ThemeColor.Red => red,
            ThemeColor.RedShadow => redShadow,
            ThemeColor.Ink => ink,
            ThemeColor.InkShadow => inkShadow,
            ThemeColor.Brown => brown,
            ThemeColor.Muted => muted,
            ThemeColor.Cream => cream,
            ThemeColor.White => white,
            ThemeColor.GreenPale => greenPale,
            ThemeColor.SegmentTrack => segmentTrack,
            ThemeColor.SliderTrack => sliderTrack,
            ThemeColor.Scrim => scrim,
            ThemeColor.Doodle => doodle,
            _ => Color.magenta,
        };

        public TMP_FontAsset GetFont(ThemeFont font) => font switch
        {
            ThemeFont.Heading => headingFont,
            ThemeFont.BodyBold => bodyBoldFont,
            _ => bodyFont,
        };

        public static ButtonPalette GetPalette(ButtonColor color) => color switch
        {
            ButtonColor.Green => new ButtonPalette(ThemeColor.Green, ThemeColor.GreenShadow, ThemeColor.White),
            ButtonColor.Yellow => new ButtonPalette(ThemeColor.Yellow, ThemeColor.YellowShadow, ThemeColor.Ink),
            ButtonColor.Blue => new ButtonPalette(ThemeColor.Blue, ThemeColor.BlueShadow, ThemeColor.White),
            ButtonColor.Red => new ButtonPalette(ThemeColor.Red, ThemeColor.RedShadow, ThemeColor.White),
            ButtonColor.WhiteOnTeal => new ButtonPalette(ThemeColor.White, ThemeColor.TealShadow, ThemeColor.Ink),
            _ => new ButtonPalette(ThemeColor.White, ThemeColor.YellowShadow, ThemeColor.Ink),
        };

        public static Color Hex(int rgb, float alpha = 1f) =>
            new Color(((rgb >> 16) & 0xFF) / 255f, ((rgb >> 8) & 0xFF) / 255f, (rgb & 0xFF) / 255f, alpha);
    }
}
