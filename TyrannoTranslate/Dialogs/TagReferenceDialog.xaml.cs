using System.Windows;
using System.Windows.Controls;
using System.Windows.Documents;
using System.Windows.Media;
using System.Windows.Shapes;

namespace TyrannoTranslate.Dialogs;

public partial class TagReferenceDialog : Window
{
    public TagReferenceDialog(Window owner)
    {
        Owner = owner;
        InitializeComponent();
        BuildContent();
    }

    private void BuildContent()
    {
        AddSection("Text Flow",
            ("[p]", "Wait for click, clear text box, advance to next page"),
            ("[l]", "Wait for click, continue on same line"),
            ("[r]", "Line break (new paragraph)"),
            ("[lr]", "Wait for click, then line break"),
            ("[cm]", "Clear all message layers"),
            ("[er]", "Erase text on current message layer"),
            ("[ct]", "Reset message layer (keeps backlog)")
        );

        AddSection("Character & Names",
            ("#名前", "Character name marker at start of a line (e.g. #主人公)"),
            ("[chara_show]", "Show a character sprite on screen"),
            ("[chara_hide]", "Hide a character sprite"),
            ("[chara_mod]", "Change character appearance/expression"),
            ("[chara_move]", "Change character position on screen")
        );

        AddSection("Choices & Branches",
            ("[link]...[endlink]", "Hyperlink / choice text (clickable jump)"),
            ("[button]", "Display a graphical clickable button"),
            ("[if]/[else]/[endif]", "Conditional branching"),
            ("[jump]", "Jump to a different scenario section (*label)"),
            ("[call]/[return]", "Call a subroutine and return")
        );

        AddSection("Audio",
            ("[playbgm]", "Play background music"),
            ("[stopbgm]", "Stop background music"),
            ("[fadeinbgm]/[fadeoutbgm]", "Fade background music in/out"),
            ("[playse]", "Play a sound effect"),
            ("[stopse]", "Stop a sound effect")
        );

        AddSection("Visual & Effects",
            ("[bg]", "Switch background image"),
            ("[image]", "Display an image on a layer"),
            ("[freeimage]", "Clear/remove an image layer"),
            ("[trans]", "Layer transition effect (fade, wipe, etc.)"),
            ("[quake]", "Shake the screen"),
            ("[font]", "Change text style (size, color, bold, etc.)"),
            ("[ruby]", "Add furigana reading text above kanji"),
            ("[layopt]", "Set layer options (visibility, position)")
        );

        AddSection("System",
            ("[s]", "Stop the game"),
            ("[wait]", "Wait for specified time (milliseconds)"),
            ("[close]", "Close the game window"),
            ("[showsave]/[showload]", "Display save / load screen"),
            ("[iscript]...[endscript]", "Embed JavaScript code"),
            ("[emb]", "Embed expression result into text"),
            ("*label", "Section label (target for [jump] and choices)"),
            (";", "Comment line (ignored by engine)")
        );

        var header = new TextBlock
        {
            Text = "Tips",
            FontSize = 17,
            FontWeight = FontWeights.SemiBold,
            Foreground = FindResource("Accent") as Brush ?? Brushes.CornflowerBlue,
            Margin = new Thickness(0, 18, 0, 8)
        };
        ContentPanel.Children.Add(header);

        AddTip("Tags inside [brackets] must be preserved exactly in translations — the engine needs them to function.");
        AddTip("The # character at line start marks a speaker name. Translate the name, keep the #.");
        AddTip("Lines starting with * are labels (not dialogue). Don't translate them.");
        AddTip("Lines starting with ; are comments. You can leave them as-is or translate them for reference.");
        AddTip("When in doubt, preserve all [...] tags and let the game engine handle rendering.");

        ContentPanel.Children.Add(new Rectangle
        {
            Height = 1,
            Fill = FindResource("BorderColor") as Brush ?? Brushes.Gray,
            Margin = new Thickness(0, 18, 0, 0)
        });
    }

    private void AddSection(string title, params (string tag, string desc)[] rows)
    {
        var header = new TextBlock
        {
            Text = title,
            FontSize = 17,
            FontWeight = FontWeights.SemiBold,
            Foreground = FindResource("Accent") as Brush ?? Brushes.CornflowerBlue,
            Margin = new Thickness(0, 0, 0, 8)
        };
        ContentPanel.Children.Add(header);

        foreach (var (tag, desc) in rows)
        {
            var grid = new Grid { Margin = new Thickness(0, 0, 0, 4) };
            grid.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(150) });
            grid.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(1, GridUnitType.Star) });

            var tagBorder = new Border
            {
                BorderBrush = FindResource("BorderColor") as Brush ?? Brushes.Gray,
                BorderThickness = new Thickness(1),
                CornerRadius = new CornerRadius(3),
                Padding = new Thickness(8, 3, 8, 3),
                Background = FindResource("BgPanel") as Brush ?? Brushes.DimGray,
                VerticalAlignment = VerticalAlignment.Center,
                Child = new TextBlock
                {
                    Text = tag,
                    FontFamily = new FontFamily("Consolas"),
                    FontSize = 13,
                    Foreground = FindResource("Accent") as Brush ?? Brushes.CornflowerBlue
                }
            };
            Grid.SetColumn(tagBorder, 0);
            grid.Children.Add(tagBorder);

            var descText = new TextBlock
            {
                Text = desc,
                TextWrapping = TextWrapping.Wrap,
                FontSize = 13,
                Foreground = FindResource("TextPrimary") as Brush ?? Brushes.White,
                VerticalAlignment = VerticalAlignment.Center,
                Margin = new Thickness(8, 0, 0, 0)
            };
            Grid.SetColumn(descText, 1);
            grid.Children.Add(descText);

            ContentPanel.Children.Add(grid);
        }
    }

    private void AddTip(string text)
    {
        var tip = new TextBlock
        {
            TextWrapping = TextWrapping.Wrap,
            Foreground = FindResource("TextMuted") as Brush ?? Brushes.Gray,
            FontSize = 13,
            LineHeight = 20,
            Margin = new Thickness(0, 0, 0, 4)
        };
        tip.Inlines.Add(new Run("• "));
        tip.Inlines.Add(new Run(text));
        ContentPanel.Children.Add(tip);
    }

    private void Close_Click(object sender, RoutedEventArgs e)
    {
        DialogResult = true;
        Close();
    }
}
