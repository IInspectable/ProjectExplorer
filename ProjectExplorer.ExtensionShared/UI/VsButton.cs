using System.Windows;
using System.Windows.Controls;

namespace IInspectable.ProjectExplorer.Extension.UI {

    public class VsButton: Button {

        static VsButton() {
            #if VS2022
            // Styles funktionieren (noch?) nicht in VS 2022
            #else
            DefaultStyleKeyProperty.OverrideMetadata(typeof(VsButton), new FrameworkPropertyMetadata(typeof(VsButton)));
            #endif
        }

        #region DependencyProperty Glyph

        /// <summary>
        /// Registers a dependency property as backing store for the Glyph property
        /// </summary>
        public static readonly DependencyProperty GlyphProperty =
                DependencyProperty.Register(nameof(Glyph), typeof(object), typeof(VsButton),
                        new FrameworkPropertyMetadata(null,
                                FrameworkPropertyMetadataOptions.AffectsRender |
                                FrameworkPropertyMetadataOptions.AffectsParentMeasure));

        /// <summary>
        /// Gets or sets the Glyph.
        /// </summary>
        /// <value>The Glyph.</value>
        public object Glyph {
            get => GetValue(GlyphProperty);
            set => SetValue(GlyphProperty, value);
        }

        #endregion
    }
}
