using System;
using System.Collections.Generic;
using System.Collections.Specialized;
using System.ComponentModel;
using System.Linq;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Controls.Primitives;
using System.Windows.Input;
using System.Windows.Media;
using Module.VideoWall.WPF.Api.Direct.Isapi;
using Module.VideoWall.WPF.Api.Dto;
using Module.VideoWall.WPF.ViewModels;

namespace Module.VideoWall.WPF.Controls;

/// <summary>
/// Description: Điều khiển trực quan (UserControl) hiển thị ma trận tường ghép và kéo thả bố cục các cửa sổ
/// Created date: 13/09/2026
/// </summary>
public partial class VisualWallCanvas : UserControl
{
    public const double DisplayHeightRatio = 1080.0 / 1920.0; // 0.5625 (Physical 16:9 widescreen ratio)

    private double _scale = 0.1;
    private int _wallTotalWidth = 3840;
    private int _wallTotalHeight = 3840;

    private readonly Dictionary<SceneWindowRow, Border> _windowCards = new();
    private bool _isDragging;
    private SceneWindowRow? _activeDraggedRow;
    private Point _dragStartPoint;
    private int _initialX;
    private int _initialY;

    public VisualWallCanvas()
    {
        InitializeComponent();
        DataContextChanged += OnDataContextChanged;
        Loaded += OnLoaded;
    }

    private SceneSetupViewModel? Vm => DataContext as SceneSetupViewModel;

    private int Tile => Vm?.Tile > 0 ? Vm.Tile : VwDirectDeviceConstants.UniformTileSize;

    private void OnLoaded(object sender, RoutedEventArgs e)
    {
        RedrawCanvas();
    }

    private void OnDataContextChanged(object sender, DependencyPropertyChangedEventArgs e)
    {
        if (e.OldValue is SceneSetupViewModel oldVm)
        {
            oldVm.PropertyChanged -= OnViewModelPropertyChanged;
            oldVm.SceneWindows.CollectionChanged -= OnSceneWindowsCollectionChanged;
            oldVm.AvailableStartScreens.CollectionChanged -= OnAvailableStartScreensCollectionChanged;
            foreach (var row in oldVm.SceneWindows)
                row.PropertyChanged -= OnRowPropertyChanged;
        }

        if (e.NewValue is SceneSetupViewModel newVm)
        {
            newVm.PropertyChanged += OnViewModelPropertyChanged;
            newVm.SceneWindows.CollectionChanged += OnSceneWindowsCollectionChanged;
            newVm.AvailableStartScreens.CollectionChanged += OnAvailableStartScreensCollectionChanged;
            foreach (var row in newVm.SceneWindows)
                row.PropertyChanged += OnRowPropertyChanged;
        }

        RedrawCanvas();
    }

    private void OnViewModelPropertyChanged(object? sender, PropertyChangedEventArgs e)
    {
        if (e.PropertyName is nameof(SceneSetupViewModel.CurrentScene)
            or nameof(SceneSetupViewModel.IsCreatingNewScene)
            or nameof(SceneSetupViewModel.TotalWallWidth)
            or nameof(SceneSetupViewModel.TotalWallHeight)
            or nameof(SceneSetupViewModel.AvailableStartScreens))
        {
            RedrawCanvas();
        }
        else if (e.PropertyName is nameof(SceneSetupViewModel.SelectedSceneWindow))
        {
            UpdateAllWindowHighlights();
        }
    }

    private void OnSceneWindowsCollectionChanged(object? sender, NotifyCollectionChangedEventArgs e)
    {
        if (e.OldItems != null)
        {
            foreach (SceneWindowRow row in e.OldItems)
            {
                row.PropertyChanged -= OnRowPropertyChanged;
                if (_windowCards.TryGetValue(row, out var card))
                {
                    InteractiveWindowsCanvas.Children.Remove(card);
                    _windowCards.Remove(row);
                }
            }
        }

        if (e.NewItems != null)
        {
            foreach (SceneWindowRow row in e.NewItems)
            {
                row.PropertyChanged += OnRowPropertyChanged;
            }
        }

        SyncWindowCards();
    }

    private void OnAvailableStartScreensCollectionChanged(object? sender, NotifyCollectionChangedEventArgs e)
    {
        RedrawCanvas();
    }

    private void OnRowPropertyChanged(object? sender, PropertyChangedEventArgs e)
    {
        if (sender is not SceneWindowRow row)
            return;

        if (_windowCards.TryGetValue(row, out var card))
        {
            UpdateWindowCardGeometryAndStyle(card, row);
        }
        else
        {
            SyncWindowCards();
        }
    }

    private void OnRootContainerSizeChanged(object sender, SizeChangedEventArgs e)
    {
        RedrawCanvas();
    }

    public void RedrawCanvas()
    {
        if (Vm == null)
        {
            EmptyOverlay.Visibility = Visibility.Visible;
            return;
        }

        EmptyOverlay.Visibility = (Vm.CurrentScene == null && !Vm.IsCreatingNewScene) ? Visibility.Visible : Visibility.Collapsed;

        _wallTotalWidth = Vm.TotalWallWidth > 0 ? Vm.TotalWallWidth : 3840;
        _wallTotalHeight = Vm.TotalWallHeight > 0 ? Vm.TotalWallHeight : 3840;

        var availableW = Math.Max(200, RootContainerGrid.ActualWidth - 24);
        var availableH = Math.Max(120, RootContainerGrid.ActualHeight - 24);

        var displayTotalH = _wallTotalHeight * DisplayHeightRatio;

        var scaleX = availableW / _wallTotalWidth;
        var scaleY = availableH / displayTotalH;
        _scale = Math.Min(scaleX, scaleY);
        if (_scale <= 0.001 || double.IsNaN(_scale) || double.IsInfinity(_scale))
            _scale = 0.08;

        var viewportW = _wallTotalWidth * _scale;
        var viewportH = displayTotalH * _scale;

        WallViewportCanvas.Width = viewportW;
        WallViewportCanvas.Height = viewportH;
        PhysicalScreensCanvas.Width = viewportW;
        PhysicalScreensCanvas.Height = viewportH;
        InteractiveWindowsCanvas.Width = viewportW;
        InteractiveWindowsCanvas.Height = viewportH;

        var cols = Math.Max(1, _wallTotalWidth / Tile);
        var rows = Math.Max(1, _wallTotalHeight / Tile);
        ScaleInfoText.Text = FormatScaleInfo(_scale, cols, rows, _wallTotalWidth, _wallTotalHeight);

        DrawPhysicalScreens();
        SyncWindowCards();
    }

    public static string FormatScaleInfo(double scale, int cols, int rows, int wallTotalWidth, int wallTotalHeight)
    {
        return $"📐 Preview: {scale:P0} · Lưới {cols} × {rows} ({cols * rows} Màn hình 16:9) · Khung tọa độ: X[0 ➔ {wallTotalWidth} px] × Y[0 ➔ {wallTotalHeight} px]";
    }

    public static string FormatScreenCoordinateRange(int startX, int startY, int tileSize = VwDirectDeviceConstants.UniformTileSize)
    {
        var endX = startX + tileSize;
        var endY = startY + tileSize;
        return $"X: {startX} ➔ {endX}\nY: {startY} ➔ {endY}";
    }

    private void DrawPhysicalScreens()
    {
        PhysicalScreensCanvas.Children.Clear();
        var screens = Vm?.AvailableStartScreens;

        if (screens != null && screens.Count > 0)
        {
            foreach (var screen in screens)
            {
                var screenLeft = screen.X * _scale;
                var screenTop = screen.Y * DisplayHeightRatio * _scale;
                var screenW = Tile * _scale;
                var screenH = Tile * DisplayHeightRatio * _scale;
                var endX = screen.X + Tile;
                var endY = screen.Y + Tile;

                var screenBorder = new Border
                {
                    Width = screenW,
                    Height = screenH,
                    Background = new SolidColorBrush(Color.FromRgb(0x1E, 0x29, 0x3B)),
                    BorderBrush = new SolidColorBrush(Color.FromRgb(0x47, 0x55, 0x69)),
                    BorderThickness = new Thickness(1.5),
                    CornerRadius = new CornerRadius(3),
                    ToolTip = $"🖥️ {screen.Name} (Cột {screen.Col}, Hàng {screen.Row})\n" +
                              $"• Tọa độ X: {screen.X} ➔ {endX} px\n" +
                              $"• Tọa độ Y: {screen.Y} ➔ {endY} px\n" +
                              $"• Kích thước: {Tile} × {Tile} px",
                };

                var contentStack = new StackPanel
                {
                    HorizontalAlignment = HorizontalAlignment.Center,
                    VerticalAlignment = VerticalAlignment.Center,
                };

                contentStack.Children.Add(new TextBlock
                {
                    Text = $"🖥️ {screen.Name}",
                    FontSize = Math.Max(9, Math.Min(12, 11 * (_scale / 0.1))),
                    FontWeight = FontWeights.Bold,
                    Foreground = new SolidColorBrush(Color.FromRgb(0x94, 0xA3, 0xB8)),
                    HorizontalAlignment = HorizontalAlignment.Center,
                });

                contentStack.Children.Add(new TextBlock
                {
                    Text = FormatScreenCoordinateRange(screen.X, screen.Y, Tile),
                    FontSize = Math.Max(7.5, Math.Min(9.5, 8.5 * (_scale / 0.1))),
                    Foreground = new SolidColorBrush(Color.FromRgb(0x64, 0x74, 0x8B)),
                    FontFamily = new FontFamily("Consolas"),
                    TextAlignment = TextAlignment.Center,
                    HorizontalAlignment = HorizontalAlignment.Center,
                    Margin = new Thickness(0, 2, 0, 0),
                });

                screenBorder.Child = contentStack;
                Canvas.SetLeft(screenBorder, screenLeft);
                Canvas.SetTop(screenBorder, screenTop);
                PhysicalScreensCanvas.Children.Add(screenBorder);
            }
        }
        else
        {
            var fallbackGrid = new Border
            {
                Width = WallViewportCanvas.Width,
                Height = WallViewportCanvas.Height,
                Background = new SolidColorBrush(Color.FromRgb(0x1E, 0x29, 0x3B)),
                BorderBrush = new SolidColorBrush(Color.FromRgb(0x47, 0x55, 0x69)),
                BorderThickness = new Thickness(1),
            };
            Canvas.SetLeft(fallbackGrid, 0);
            Canvas.SetTop(fallbackGrid, 0);
            PhysicalScreensCanvas.Children.Add(fallbackGrid);
        }
    }

    private void SyncWindowCards()
    {
        if (Vm?.SceneWindows == null)
        {
            InteractiveWindowsCanvas.Children.Clear();
            _windowCards.Clear();
            return;
        }

        var currentRows = Vm.SceneWindows.ToList();

        var toRemove = _windowCards.Keys.Where(k => !currentRows.Contains(k)).ToList();
        foreach (var r in toRemove)
        {
            if (_windowCards.TryGetValue(r, out var card))
            {
                InteractiveWindowsCanvas.Children.Remove(card);
                _windowCards.Remove(r);
            }
        }

        foreach (var row in currentRows)
        {
            if (!_windowCards.TryGetValue(row, out var card))
            {
                card = CreateWindowCard(row);
                _windowCards[row] = card;
                InteractiveWindowsCanvas.Children.Add(card);
            }
            UpdateWindowCardGeometryAndStyle(card, row);
        }
    }

    private Border CreateWindowCard(SceneWindowRow row)
    {
        var container = new Border
        {
            CornerRadius = new CornerRadius(3),
            Cursor = Cursors.SizeAll,
            Tag = row,
        };

        var rootGrid = new Grid();
        rootGrid.RowDefinitions.Add(new RowDefinition { Height = GridLength.Auto });
        rootGrid.RowDefinitions.Add(new RowDefinition { Height = new GridLength(1, GridUnitType.Star) });

        // Header bar
        var headerBar = new Border
        {
            Name = "HeaderBar",
            Padding = new Thickness(4, 2, 4, 2),
        };

        var headerGrid = new Grid();
        headerGrid.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(1, GridUnitType.Star) });
        headerGrid.ColumnDefinitions.Add(new ColumnDefinition { Width = GridLength.Auto });

        var screenTitle = !string.IsNullOrWhiteSpace(row.Name) ? row.Name : (row.SelectedStartScreen?.Name != null ? $"Ô {row.OrderNo} · {row.SelectedStartScreen.Name}" : (row.Label ?? $"Ô {row.OrderNo}"));
        var titleText = new TextBlock
        {
            Name = "TitleText",
            Text = $"📹 {screenTitle}",
            FontSize = 10,
            FontWeight = FontWeights.Bold,
            Foreground = Brushes.White,
            TextTrimming = TextTrimming.CharacterEllipsis,
            VerticalAlignment = VerticalAlignment.Center,
        };
        Grid.SetColumn(titleText, 0);
        headerGrid.Children.Add(titleText);

        var zBadge = new Border
        {
            Name = "ZBadge",
            CornerRadius = new CornerRadius(2),
            Padding = new Thickness(3, 0, 3, 0),
            Margin = new Thickness(4, 0, 0, 0),
            VerticalAlignment = VerticalAlignment.Center,
        };
        var zText = new TextBlock
        {
            Name = "ZText",
            FontSize = 9,
            FontWeight = FontWeights.Bold,
            Foreground = Brushes.White,
        };
        zBadge.Child = zText;
        Grid.SetColumn(zBadge, 1);
        headerGrid.Children.Add(zBadge);

        headerBar.Child = headerGrid;
        Grid.SetRow(headerBar, 0);
        rootGrid.Children.Add(headerBar);

        // Body area
        var bodyStack = new StackPanel
        {
            HorizontalAlignment = HorizontalAlignment.Center,
            VerticalAlignment = VerticalAlignment.Center,
            IsHitTestVisible = false,
        };

        var sourceText = new TextBlock
        {
            Name = "SourceText",
            FontSize = 9.5,
            FontWeight = FontWeights.SemiBold,
            HorizontalAlignment = HorizontalAlignment.Center,
            TextAlignment = TextAlignment.Center,
            TextWrapping = TextWrapping.Wrap,
            Margin = new Thickness(4, 1, 4, 1),
            MaxHeight = 32,
            TextTrimming = TextTrimming.CharacterEllipsis,
        };
        bodyStack.Children.Add(sourceText);

        var sizeText = new TextBlock
        {
            Name = "SizeText",
            FontSize = 8.5,
            Foreground = new SolidColorBrush(Color.FromRgb(0x94, 0xA3, 0xB8)),
            FontFamily = new FontFamily("Consolas"),
            HorizontalAlignment = HorizontalAlignment.Center,
            Margin = new Thickness(0, 1, 0, 0),
        };
        bodyStack.Children.Add(sizeText);

        Grid.SetRow(bodyStack, 1);
        rootGrid.Children.Add(bodyStack);

        var subGrid = new UniformGrid
        {
            Name = "SubGrid",
            Margin = new Thickness(2),
            Visibility = Visibility.Collapsed,
        };
        Grid.SetRow(subGrid, 1);
        rootGrid.Children.Add(subGrid);

        // Resize Thumb
        var resizeThumb = new Thumb
        {
            Name = "ResizeThumb",
            Width = 12,
            Height = 12,
            HorizontalAlignment = HorizontalAlignment.Right,
            VerticalAlignment = VerticalAlignment.Bottom,
            Cursor = Cursors.SizeNWSE,
            Background = new SolidColorBrush(Color.FromRgb(0xFB, 0xBF, 0x24)),
            BorderBrush = Brushes.White,
            BorderThickness = new Thickness(1),
            Margin = new Thickness(0, 0, -1, -1),
        };
        Grid.SetRowSpan(resizeThumb, 2);

        resizeThumb.DragDelta += (s, e) =>
        {
            var deltaW = e.HorizontalChange / _scale;
            var deltaH = e.VerticalChange / (_scale * DisplayHeightRatio);

            var newW = Math.Max(640, (int)Math.Round((row.W ?? Tile) + deltaW));
            var newH = Math.Max(360, (int)Math.Round((row.H ?? Tile) + deltaH));

            newW = Math.Min(_wallTotalWidth - (row.X ?? 0), newW);
            newH = Math.Min(_wallTotalHeight - (row.Y ?? 0), newH);

            row.W = newW;
            row.H = newH;
        };

        resizeThumb.DragStarted += (s, e) =>
        {
            row.IsInteractivelyDragging = true;
        };

        resizeThumb.DragCompleted += (s, e) =>
        {
            row.W = SnapSize(row.W ?? Tile, Tile);
            row.H = SnapSize(row.H ?? Tile, Tile);
            row.IsInteractivelyDragging = false;
            Vm?.TriggerImmediateLiveWindowUpdate(row);
        };

        rootGrid.Children.Add(resizeThumb);
        container.Child = rootGrid;

        container.AllowDrop = true;
        container.DragOver += (s, e) =>
        {
            if (e.Data.GetDataPresent(typeof(VwSourceDto)) || e.Data.GetDataPresent(typeof(Api.Direct.Isapi.VwISAPIInputChannel)) || e.Data.GetDataPresent(typeof(Api.Dto.VwISAPIInputChannel)) || e.Data.GetDataPresent(DataFormats.Text))
            {
                e.Effects = DragDropEffects.Copy;
            }
            else
            {
                e.Effects = DragDropEffects.None;
            }
            e.Handled = true;
        };

        container.Drop += (s, e) =>
        {
            var droppedSource = ExtractSourceFromDrag(e.Data);
            if (droppedSource != null)
            {
                if (row.WindowMode > 1)
                {
                    var targetCell = row.SubCells.FirstOrDefault(c => c.SelectedSource == null) ?? row.SubCells.FirstOrDefault();
                    if (targetCell != null)
                    {
                        targetCell.SelectedSource = droppedSource;
                    }
                }
                else
                {
                    row.SelectedSource = droppedSource;
                }
                Vm?.MarkSceneWindowsDirty();
                UpdateWindowCardGeometryAndStyle(container, row);
                e.Handled = true;
            }
        };

        // Mouse Drag to Move
        container.MouseLeftButtonDown += (s, e) =>
        {
            if (Vm != null)
                Vm.SelectedSceneWindow = row;

            _isDragging = true;
            _activeDraggedRow = row;
            row.IsInteractivelyDragging = true;
            _dragStartPoint = e.GetPosition(WallViewportCanvas);
            _initialX = row.X ?? 0;
            _initialY = row.Y ?? 0;

            container.CaptureMouse();
            e.Handled = true;
        };

        container.MouseMove += (s, e) =>
        {
            if (!_isDragging || _activeDraggedRow != row)
                return;

            var currentPoint = e.GetPosition(WallViewportCanvas);
            var deltaX = (currentPoint.X - _dragStartPoint.X) / _scale;
            var deltaY = (currentPoint.Y - _dragStartPoint.Y) / (_scale * DisplayHeightRatio);

            var newX = (int)Math.Round(_initialX + deltaX);
            var newY = (int)Math.Round(_initialY + deltaY);

            var maxX = Math.Max(0, _wallTotalWidth - (row.W ?? Tile));
            var maxY = Math.Max(0, _wallTotalHeight - (row.H ?? Tile));

            newX = Math.Max(0, Math.Min(maxX, newX));
            newY = Math.Max(0, Math.Min(maxY, newY));

            Canvas.SetLeft(container, newX * _scale);
            Canvas.SetTop(container, newY * DisplayHeightRatio * _scale);

            row.X = newX;
            row.Y = newY;
        };

        container.MouseLeftButtonUp += (s, e) =>
        {
            if (_isDragging && _activeDraggedRow == row)
            {
                container.ReleaseMouseCapture();
                _isDragging = false;
                _activeDraggedRow = null;
                var snappedX = SnapToGrid(row.X ?? 0, Tile);
                var snappedY = SnapToGrid(row.Y ?? 0, Tile);
                var maxX = Math.Max(0, _wallTotalWidth - (row.W ?? Tile));
                var maxY = Math.Max(0, _wallTotalHeight - (row.H ?? Tile));

                row.X = Math.Max(0, Math.Min(maxX, snappedX));
                row.Y = Math.Max(0, Math.Min(maxY, snappedY));
                row.IsInteractivelyDragging = false;

                row.UpdateLabelFromCurrentPosition();
                UpdateWindowCardGeometryAndStyle(container, row);
                e.Handled = true;

                Vm?.TriggerImmediateLiveWindowUpdate(row);
            }
        };

        return container;
    }

    private void UpdateWindowCardGeometryAndStyle(Border card, SceneWindowRow row)
    {
        var x = (row.X ?? 0) * _scale;
        var y = (row.Y ?? 0) * DisplayHeightRatio * _scale;
        var w = Math.Max(30, (row.W ?? Tile) * _scale);
        var h = Math.Max(20, (row.H ?? Tile) * DisplayHeightRatio * _scale);
        var isSelected = row == Vm?.SelectedSceneWindow || row.IsActiveSelected;

        Canvas.SetLeft(card, x);
        Canvas.SetTop(card, y);
        card.Width = w;
        card.Height = h;

        card.Background = isSelected
            ? new SolidColorBrush(Color.FromArgb(0xEE, 0x1E, 0x40, 0xAF))
            : new SolidColorBrush(Color.FromArgb(0xCC, 0x1E, 0x29, 0x3B));
        card.BorderBrush = isSelected
            ? new SolidColorBrush(Color.FromRgb(0xFB, 0xBF, 0x24))
            : new SolidColorBrush(Color.FromRgb(0x3B, 0x82, 0xF6));
        card.BorderThickness = new Thickness(isSelected ? 2.5 : 1.5);
        Panel.SetZIndex(card, CalculateCardZIndex(row.ZIndex, isSelected, row.OrderNo));
        card.ToolTip = $"Ô {row.OrderNo}: {(!string.IsNullOrWhiteSpace(row.Name) ? row.Name : (row.Label ?? $"Ô {row.OrderNo}"))}\n" +
                       $"• Màn bắt đầu: {row.SelectedStartScreen?.Name ?? "(Chưa chọn)"}\n" +
                       $"• Nguồn video: {row.SelectedSource?.Name ?? "(Chưa gán)"}\n" +
                       $"• Tọa độ ISAPI: ({row.X}, {row.Y})\n" +
                       $"• Kích thước: {row.W} × {row.H} px (Z-Index: {row.ZIndex ?? 1})";

        if (card.Child is Grid grid)
        {
            var headerBar = grid.Children.OfType<Border>().FirstOrDefault(b => b.Name == "HeaderBar");
            if (headerBar != null)
            {
                headerBar.Background = isSelected
                    ? new SolidColorBrush(Color.FromRgb(0x1D, 0x4E, 0xD8))
                    : new SolidColorBrush(Color.FromRgb(0x0F, 0x17, 0x2A));

                if (headerBar.Child is Grid headerGrid)
                {
                    var titleText = headerGrid.Children.OfType<TextBlock>().FirstOrDefault(t => t.Name == "TitleText");
                    if (titleText != null)
                    {
                        var screenTitle = !string.IsNullOrWhiteSpace(row.Name) ? row.Name : (row.SelectedStartScreen?.Name != null ? $"Ô {row.OrderNo} · {row.SelectedStartScreen.Name}" : (row.Label ?? $"Ô {row.OrderNo}"));
                        titleText.Text = $"📹 {screenTitle}";
                    }

                    var zBadge = headerGrid.Children.OfType<Border>().FirstOrDefault(b => b.Name == "ZBadge");
                    if (zBadge != null)
                    {
                        zBadge.Background = isSelected
                            ? new SolidColorBrush(Color.FromRgb(0xD9, 0x77, 0x06))
                            : new SolidColorBrush(Color.FromRgb(0x33, 0x41, 0x55));

                        if (zBadge.Child is TextBlock zText)
                            zText.Text = $"Z:{row.ZIndex ?? 1}";
                    }
                }
            }

            var bodyStack = grid.Children.OfType<StackPanel>().FirstOrDefault();
            var subGrid = grid.Children.OfType<UniformGrid>().FirstOrDefault(u => u.Name == "SubGrid");

            if (row.WindowMode > 1)
            {
                if (bodyStack != null)
                    bodyStack.Visibility = Visibility.Collapsed;

                if (subGrid != null)
                {
                    subGrid.Visibility = Visibility.Visible;
                    var side = (int)Math.Max(1, Math.Round(Math.Sqrt(row.WindowMode)));
                    subGrid.Rows = side;
                    subGrid.Columns = side;

                    if (subGrid.Children.Count != row.WindowMode)
                    {
                        subGrid.Children.Clear();
                        for (var i = 1; i <= row.WindowMode; i++)
                        {
                            var cellIndex = i;
                            var cellBorder = new Border
                            {
                                Name = $"SubCell_{cellIndex}",
                                Tag = cellIndex,
                                CornerRadius = new CornerRadius(2),
                                Margin = new Thickness(1),
                                Padding = new Thickness(2),
                                BorderThickness = new Thickness(1),
                                AllowDrop = true,
                            };

                            var cellContent = new Grid();
                            cellContent.RowDefinitions.Add(new RowDefinition { Height = GridLength.Auto });
                            cellContent.RowDefinitions.Add(new RowDefinition { Height = new GridLength(1, GridUnitType.Star) });

                            var idxText = new TextBlock
                            {
                                Name = "CellIdxText",
                                Text = $"#{cellIndex}",
                                FontSize = 8,
                                FontWeight = FontWeights.Bold,
                                Foreground = new SolidColorBrush(Color.FromRgb(0x94, 0xA3, 0xB8)),
                                HorizontalAlignment = HorizontalAlignment.Left,
                            };
                            Grid.SetRow(idxText, 0);
                            cellContent.Children.Add(idxText);

                            var nameText = new TextBlock
                            {
                                Name = "CellNameText",
                                FontSize = 8.5,
                                FontWeight = FontWeights.SemiBold,
                                HorizontalAlignment = HorizontalAlignment.Center,
                                VerticalAlignment = VerticalAlignment.Center,
                                TextAlignment = TextAlignment.Center,
                                TextTrimming = TextTrimming.CharacterEllipsis,
                                TextWrapping = TextWrapping.NoWrap,
                            };
                            Grid.SetRow(nameText, 1);
                            cellContent.Children.Add(nameText);

                            cellBorder.Child = cellContent;

                            cellBorder.DragOver += (s, e) =>
                            {
                                if (e.Data.GetDataPresent(typeof(VwSourceDto)) || e.Data.GetDataPresent(typeof(Api.Direct.Isapi.VwISAPIInputChannel)) || e.Data.GetDataPresent(typeof(Api.Dto.VwISAPIInputChannel)) || e.Data.GetDataPresent(DataFormats.Text))
                                {
                                    e.Effects = DragDropEffects.Copy;
                                    cellBorder.Background = new SolidColorBrush(Color.FromArgb(0x88, 0x25, 0x63, 0xEB));
                                }
                                else
                                {
                                    e.Effects = DragDropEffects.None;
                                }
                                e.Handled = true;
                            };

                            cellBorder.Drop += (s, e) =>
                            {
                                var src = ExtractSourceFromDrag(e.Data);
                                if (src != null)
                                {
                                    var cellModel = row.SubCells.FirstOrDefault(c => c.Index == cellIndex);
                                    if (cellModel != null)
                                    {
                                        cellModel.SelectedSource = src;
                                    }
                                    Vm?.MarkSceneWindowsDirty();
                                    UpdateWindowCardGeometryAndStyle(card, row);
                                    e.Handled = true;
                                }
                            };

                            subGrid.Children.Add(cellBorder);
                        }
                    }

                    foreach (var child in subGrid.Children)
                    {
                        if (child is Border b && b.Tag is int cIdx)
                        {
                            var subCellModel = row.SubCells.FirstOrDefault(c => c.Index == cIdx);
                            var nameText = (b.Child as Grid)?.Children.OfType<TextBlock>().FirstOrDefault(t => t.Name == "CellNameText");
                            if (subCellModel?.SelectedSource != null)
                            {
                                b.Background = isSelected
                                    ? new SolidColorBrush(Color.FromArgb(0x88, 0x1E, 0x40, 0xAF))
                                    : new SolidColorBrush(Color.FromArgb(0x55, 0x25, 0x63, 0xEB));
                                b.BorderBrush = new SolidColorBrush(Color.FromRgb(0x60, 0xA5, 0xFA));
                                if (nameText != null)
                                {
                                    nameText.Text = subCellModel.SelectedSource.Name;
                                    nameText.Foreground = Brushes.White;
                                    nameText.FontStyle = FontStyles.Normal;
                                }
                            }
                            else
                            {
                                b.Background = new SolidColorBrush(Color.FromArgb(0x33, 0x0F, 0x17, 0x2A));
                                b.BorderBrush = new SolidColorBrush(Color.FromArgb(0x44, 0x64, 0x74, 0x8B));
                                if (nameText != null)
                                {
                                    nameText.Text = "(Trống)";
                                    nameText.Foreground = new SolidColorBrush(Color.FromRgb(0x94, 0xA3, 0xB8));
                                    nameText.FontStyle = FontStyles.Italic;
                                }
                            }
                        }
                    }
                }
            }
            else
            {
                if (subGrid != null)
                    subGrid.Visibility = Visibility.Collapsed;

                if (bodyStack != null)
                {
                    bodyStack.Visibility = Visibility.Visible;
                    var sourceText = bodyStack.Children.OfType<TextBlock>().FirstOrDefault(t => t.Name == "SourceText");
                    if (sourceText != null)
                    {
                        sourceText.Text = row.SelectedSource?.Name ?? "(Chưa gán nguồn)";
                        sourceText.Foreground = isSelected ? Brushes.White : new SolidColorBrush(Color.FromRgb(0x93, 0xC5, 0xFD));
                    }

                    var sizeText = bodyStack.Children.OfType<TextBlock>().FirstOrDefault(t => t.Name == "SizeText");
                    if (sizeText != null)
                        sizeText.Text = $"{row.W} × {row.H} px";
                }
            }

            var resizeThumb = grid.Children.OfType<Thumb>().FirstOrDefault(t => t.Name == "ResizeThumb");
            if (resizeThumb != null)
                resizeThumb.Visibility = isSelected ? Visibility.Visible : Visibility.Collapsed;
        }
    }

    private VwSourceDto? ExtractSourceFromDrag(IDataObject data)
    {
        if (data.GetData(typeof(VwSourceDto)) is VwSourceDto directSource)
            return directSource;

        if (data.GetData(typeof(Api.Direct.Isapi.VwISAPIInputChannel)) is Api.Direct.Isapi.VwISAPIInputChannel channelItem)
        {
            var match = Vm?.Sources.FirstOrDefault(s => s.ID == channelItem.Id.ToString() || s.SignalNo == channelItem.Id);
            if (match != null)
                return match;

            var isIp = !string.IsNullOrWhiteSpace(channelItem.InputPortType)
                       && (channelItem.InputPortType.Contains("IP", StringComparison.OrdinalIgnoreCase)
                           || channelItem.InputPortType.Contains("Stream", StringComparison.OrdinalIgnoreCase));
            var ip = channelItem.PortInBoard?.IpAddress?.Trim();
            var port = channelItem.PortInBoard?.Port > 0 ? channelItem.PortInBoard.Port : 554;
            var url = isIp && !string.IsNullOrWhiteSpace(ip)
                ? $"rtsp://{ip}:{port}/Streaming/Channels/101"
                : null;

            return new VwSourceDto
            {
                ID = channelItem.Id.ToString(),
                Name = channelItem.Name,
                SignalNo = channelItem.Id,
                SourceType = isIp ? "ip_stream" : "local_signal",
                SignalType = isIp ? (channelItem.InputPortType ?? "RTSP") : "HDMI",
                Url = url,
            };
        }

        if (data.GetData(typeof(Api.Dto.VwISAPIInputChannel)) is Api.Dto.VwISAPIInputChannel dtoItem)
        {
            var match = Vm?.Sources.FirstOrDefault(s => s.ID == dtoItem.Id.ToString() || s.SignalNo == dtoItem.Id);
            if (match != null)
                return match;

            var isIp = !string.IsNullOrWhiteSpace(dtoItem.InputPortType)
                       && (dtoItem.InputPortType.Contains("IP", StringComparison.OrdinalIgnoreCase)
                           || dtoItem.InputPortType.Contains("Stream", StringComparison.OrdinalIgnoreCase));
            var ip = dtoItem.PortInBoard?.IpAddress?.Trim();
            var port = dtoItem.PortInBoard?.Port > 0 ? dtoItem.PortInBoard.Port : 554;
            var url = isIp && !string.IsNullOrWhiteSpace(ip)
                ? $"rtsp://{ip}:{port}/Streaming/Channels/101"
                : null;

            return new VwSourceDto
            {
                ID = dtoItem.Id.ToString(),
                Name = dtoItem.Name,
                SignalNo = dtoItem.Id,
                SourceType = isIp ? "ip_stream" : "local_signal",
                SignalType = isIp ? (dtoItem.InputPortType ?? "RTSP") : "HDMI",
                Url = url,
            };
        }

        if (data.GetData(DataFormats.Text) is string text && !string.IsNullOrWhiteSpace(text))
        {
            return Vm?.Sources.FirstOrDefault(s => s.ID == text || s.Name == text || s.SignalNo.ToString() == text);
        }

        return null;
    }

    private void UpdateAllWindowHighlights()
    {
        foreach (var (row, card) in _windowCards)
        {
            UpdateWindowCardGeometryAndStyle(card, row);
        }
    }

    public static int SnapToGrid(int value, int gridSize = VwDirectDeviceConstants.UniformTileSize)
    {
        if (gridSize <= 0)
            gridSize = VwDirectDeviceConstants.UniformTileSize;
        return Math.Max(0, (int)Math.Round((double)value / gridSize) * gridSize);
    }

    public static int SnapSize(int value, int gridSize = VwDirectDeviceConstants.UniformTileSize)
    {
        if (gridSize <= 0)
            gridSize = VwDirectDeviceConstants.UniformTileSize;
        var nearest = (int)Math.Round((double)value / gridSize) * gridSize;
        return Math.Max(gridSize, nearest);
    }

    public static int CalculateCardZIndex(int? zIndex, bool isSelected, int? orderNo)
    {
        var layer = Math.Max(1, zIndex ?? 1);
        var baseZ = layer * 1000;
        var selectionZ = isSelected ? 500 : 0;
        var orderZ = Math.Min(499, Math.Max(0, orderNo ?? 0));
        return baseZ + selectionZ + orderZ;
    }
}
