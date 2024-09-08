using Microsoft.Win32;
using SkiaSharp;
using System.IO;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
using Microsoft.WindowsAPICodePack.Dialogs;
using System.Windows.Threading;
namespace TypographyGenerator {
    public partial class MainWindow : Window {
        private string selectedFontPath = null;
        private string outputFolderPath = null;
        private SKColor selectedTextColor = SKColors.White;
        private float textX = 0;
        private float textY = 0;
        private float textSizeScale = 1.0f;
        private DispatcherTimer previewTimer;
        private int currentCharIndex = 0;
        private string inputText = "";
        public MainWindow() {
            InitializeComponent();
            SelectedColorText.Text = "Selected Color: #FFFFFF";
            SelectedColorRectangle.Fill = new SolidColorBrush(System.Windows.Media.Color.FromArgb(255, 255, 255, 255));
            previewTimer = new DispatcherTimer();
            previewTimer.Interval = TimeSpan.FromMilliseconds(500);
            previewTimer.Tick += PreviewTimer_Tick;
        }
        private void PreviewTimer_Tick(object sender, EventArgs e) {            
            if (string.IsNullOrEmpty(inputText)) {
                previewTimer.Stop();
                return;
            }
            currentCharIndex = (currentCharIndex + 1) % inputText.Length;
            UpdatePreview();
        }
        protected override void OnClosed(EventArgs e) {
            previewTimer.Stop();
            base.OnClosed(e);
        }
        private void TextInput_TextChanged(object sender, TextChangedEventArgs e) {
            if (TextInput == null || previewTimer == null || PreviewCharacterInput == null)
                return;
            inputText = RemoveDuplicateCharacters(TextInput.Text.Replace("\r", "").Replace("\n", ""));
            currentCharIndex = -1;
            if (!string.IsNullOrEmpty(inputText) && string.IsNullOrEmpty(PreviewCharacterInput.Text)) {
                previewTimer.Start();
            } else {
                previewTimer.Stop();
                if (string.IsNullOrEmpty(PreviewCharacterInput.Text)) {
                    PreviewImage.Source = null;
                }
            }
        }
        private void SelectFontButton_Click(object sender, RoutedEventArgs e) {
            OpenFileDialog openFileDialog = new OpenFileDialog();
            openFileDialog.Filter = "Font files (*.ttf)|*.ttf";
            if (openFileDialog.ShowDialog() == true) {
                selectedFontPath = openFileDialog.FileName;
                SelectedFontText.Text = System.IO.Path.GetFileName(selectedFontPath);
                UpdatePreview();
            }
        }
        private void SelectColorButton_Click(object sender, RoutedEventArgs e) {
            var colorDialog = new System.Windows.Forms.ColorDialog();
            if (colorDialog.ShowDialog() == System.Windows.Forms.DialogResult.OK) {
                var color = colorDialog.Color;
                selectedTextColor = new SKColor(color.R, color.G, color.B, color.A);
                SelectedColorText.Text = $"Selected Color: #{color.R:X2}{color.G:X2}{color.B:X2}";
                SelectedColorRectangle.Fill = new SolidColorBrush(System.Windows.Media.Color.FromArgb(color.A, color.R, color.G, color.B));
                UpdatePreview();
            }
        }
        private void SelectFolderButton_Click(object sender, RoutedEventArgs e) {
            var dialog = new CommonOpenFileDialog {
                IsFolderPicker = true
            };
            if (dialog.ShowDialog() == CommonFileDialogResult.Ok) {
                outputFolderPath = dialog.FileName;
                SelectedFolderText.Text = outputFolderPath;
            }
        }
        private void TextSizeSlider_ValueChanged(object sender, RoutedPropertyChangedEventArgs<double> e) {
            textSizeScale = (float)e.NewValue;
            if (TextSizeSliderValueTextBox != null) {
                TextSizeSliderValueTextBox.Text = textSizeScale.ToString("F1");
            }
            UpdatePreview();
        }
        private void TextSizeSliderValueTextBox_TextChanged(object sender, TextChangedEventArgs e) {
            if (float.TryParse(TextSizeSliderValueTextBox.Text, out float value)) {
                textSizeScale = value;
                TextSizeSlider.Value = value;
                UpdatePreview();
            }
        }
        private void XSlider_ValueChanged(object sender, RoutedPropertyChangedEventArgs<double> e) {
            textX = (float)e.NewValue;
            if (XSliderValueTextBox != null) {
                XSliderValueTextBox.Text = textX.ToString();
            }
            UpdatePreview();
        }
        private void YSlider_ValueChanged(object sender, RoutedPropertyChangedEventArgs<double> e) {
            textY = (float)e.NewValue;
            if (YSliderValueTextBox != null) {
                YSliderValueTextBox.Text = textY.ToString();
            }
            UpdatePreview();
        }
        private void XSliderValueTextBox_TextChanged(object sender, TextChangedEventArgs e) {
            if (float.TryParse(XSliderValueTextBox.Text, out float value)) {
                textX = value;
                XSlider.Value = value;
                UpdatePreview();
            }
        }
        private void YSliderValueTextBox_TextChanged(object sender, TextChangedEventArgs e) {
            if (float.TryParse(YSliderValueTextBox.Text, out float value)) {
                textY = value;
                YSlider.Value = value;
                UpdatePreview();
            }
        }
        private void PreviewCharacterInput_TextChanged(object sender, TextChangedEventArgs e) {
            if (PreviewCharacterInput == null || TextInput == null || previewTimer == null)
                return;            
            if (!string.IsNullOrEmpty(PreviewCharacterInput.Text)) {
                previewTimer.Stop();
                currentCharIndex = -1;
                UpdatePreview();
            } else {                
                if (!previewTimer.IsEnabled && !string.IsNullOrEmpty(inputText)) {
                    previewTimer.Start();
                }
            }
        }
        private string RemoveDuplicateCharacters(string input) {
            return new string(input.Distinct().ToArray());
        }
        private void UpdatePreview() {
            if (PreviewCharacterInput == null || TextInput == null || PreviewImage == null || PreviewFilenameText == null)
                return;
            string previewChar = PreviewCharacterInput.Text.Length > 0 ? PreviewCharacterInput.Text[0].ToString() : null;
            if (string.IsNullOrEmpty(previewChar)) {
                if (string.IsNullOrEmpty(inputText) || currentCharIndex < 0) return;
                previewChar = inputText[currentCharIndex].ToString();
            }
            if (!int.TryParse(ImageWidthInput.Text, out int imageWidth) || !int.TryParse(ImageHeightInput.Text, out int imageHeight)) {
                return;
            }
            string prefix = FilenamePrefixInput.Text;
            string previewFilename = $"{prefix}{previewChar}.png";
            PreviewFilenameText.Text = $"{previewFilename}";
            var info = new SKImageInfo(imageWidth, imageHeight, SKImageInfo.PlatformColorType, SKAlphaType.Premul);
            using (var bitmap = new SKBitmap(info)) {
                using (var canvas = new SKCanvas(bitmap)) {
                    canvas.Clear(SKColors.Transparent);
                    if (!string.IsNullOrEmpty(selectedFontPath) && !string.IsNullOrEmpty(previewChar)) {
                        using (var typeface = SKTypeface.FromFile(selectedFontPath)) {
                            using (var paint = new SKPaint()) {
                                paint.Typeface = typeface;
                                paint.TextSize = imageHeight * 0.8f * textSizeScale;
                                paint.Color = selectedTextColor;
                                paint.IsAntialias = true;
                                var textBounds = new SKRect();
                                paint.MeasureText(previewChar, ref textBounds);
                                float x = (imageWidth - textBounds.Width) / 2 + textX;
                                float y = (imageHeight + textBounds.Height) / 2 + textY;
                                canvas.DrawText(previewChar, x, y, paint);
                            }
                        }
                    }
                }
                using (var image = SKImage.FromBitmap(bitmap))
                using (var data = image.Encode(SKEncodedImageFormat.Png, 100)) {
                    using (var stream = new MemoryStream()) {
                        data.SaveTo(stream);
                        stream.Seek(0, SeekOrigin.Begin);
                        var bitmapImage = new System.Windows.Media.Imaging.BitmapImage();
                        bitmapImage.BeginInit();
                        bitmapImage.StreamSource = stream;
                        bitmapImage.CacheOption = System.Windows.Media.Imaging.BitmapCacheOption.OnLoad;
                        bitmapImage.EndInit();
                        PreviewImage.Source = bitmapImage;
                    }
                }
            }
        }
        private void GenerateButton_Click(object sender, RoutedEventArgs e) {
            if (string.IsNullOrEmpty(TextInput.Text) || string.IsNullOrEmpty(selectedFontPath) || string.IsNullOrEmpty(outputFolderPath)) {
                StatusText.Text = "Please provide all inputs.";
                StatusText.Foreground = new SolidColorBrush(Colors.Red);
                return;
            }
            if (!int.TryParse(ImageWidthInput.Text, out int imageWidth) || !int.TryParse(ImageHeightInput.Text, out int imageHeight)) {
                StatusText.Text = "Invalid image size.";
                StatusText.Foreground = new SolidColorBrush(Colors.Red);
                return;
            }
            string prefix = FilenamePrefixInput.Text;
            try {
                foreach (char c in TextInput.Text) {
                    if (char.IsWhiteSpace(c)) {
                        continue;
                    }
                    string fileNameChar = c.ToString();
                    var info = new SKImageInfo(imageWidth, imageHeight, SKImageInfo.PlatformColorType, SKAlphaType.Premul);
                    using (var bitmap = new SKBitmap(info)) {
                        using (var canvas = new SKCanvas(bitmap)) {
                            canvas.Clear(SKColors.Transparent);
                            using (var typeface = SKTypeface.FromFile(selectedFontPath)) {
                                using (var paint = new SKPaint()) {
                                    paint.Typeface = typeface;
                                    paint.TextSize = imageHeight * 0.8f * textSizeScale;
                                    paint.Color = selectedTextColor;
                                    paint.IsAntialias = true;
                                    var textBounds = new SKRect();
                                    paint.MeasureText(fileNameChar, ref textBounds);
                                    float x = (imageWidth - textBounds.Width) / 2 + textX;
                                    float y = (imageHeight + textBounds.Height) / 2 + textY;
                                    canvas.DrawText(fileNameChar, x, y, paint);
                                }
                            }
                        }
                        string outputPath = System.IO.Path.Combine(outputFolderPath, $"{prefix}{fileNameChar}.png");
                        using (var image = SKImage.FromBitmap(bitmap))
                        using (var data = image.Encode(SKEncodedImageFormat.Png, 100)) {
                            using (var stream = File.OpenWrite(outputPath)) {
                                data.SaveTo(stream);
                            }
                        }
                    }
                }
                StatusText.Text = "Images generated successfully!";
                StatusText.Foreground = new SolidColorBrush(Colors.Green);
            } catch (Exception ex) {
                StatusText.Text = $"Error: {ex.Message}";
                StatusText.Foreground = new SolidColorBrush(Colors.Red);
            }
        }
    }
}