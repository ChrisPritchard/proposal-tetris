using Microsoft.Graphics.Canvas.UI;
using Microsoft.Graphics.Canvas.UI.Xaml;
using System.Collections.Generic;
using System.Threading.Tasks;
using Windows.Storage;
using Windows.UI.Core;
using Windows.UI.Xaml;
using Windows.UI.Xaml.Controls;
using Windows.UI.Xaml.Input;
using Windows.UI.Xaml.Navigation;
using System;
using Windows.UI.Xaml.Media;

namespace BlocksAndSpaces
{
    public sealed partial class MainPage : Page
    {
        Game game;
        KeyboardState keyboardState = new KeyboardState();
        bool downPressed;

        Dictionary<string, StorageFile> sounds;
        MediaElement specialSound = new MediaElement();

        public MainPage()
        {
            this.InitializeComponent();
            this.NavigationCacheMode = NavigationCacheMode.Required;

            Down.AddHandler(PointerPressedEvent, new PointerEventHandler(Down_Pressed), true);
            Down.AddHandler(PointerReleasedEvent, new PointerEventHandler(Down_Released), true);
        }

        protected override void OnNavigatedTo(NavigationEventArgs e)
        { }

        private void Canvas_CreateResources(CanvasAnimatedControl sender, CanvasCreateResourcesEventArgs args)
        {
            args.TrackAsyncAction(Setup().AsAsyncAction());
        }

        private async Task Setup()
        {
            sounds = new Dictionary<string, StorageFile>();
            var package = Windows.ApplicationModel.Package.Current;
            var installedLocation = package.InstalledLocation;

            sounds.Add("Blocked", await installedLocation.GetFileAsync("Assets\\Sounds\\Blocked.wav"));
            sounds.Add("Drop", await installedLocation.GetFileAsync("Assets\\Sounds\\Drop.wav"));
            sounds.Add("GameOver", await installedLocation.GetFileAsync("Assets\\Sounds\\GameOver.wav"));
            sounds.Add("LevelUp", await installedLocation.GetFileAsync("Assets\\Sounds\\LevelUp.wav"));
            sounds.Add("Line", await installedLocation.GetFileAsync("Assets\\Sounds\\Line.wav"));
            sounds.Add("Move", await installedLocation.GetFileAsync("Assets\\Sounds\\Move.wav"));
            sounds.Add("Rotate", await installedLocation.GetFileAsync("Assets\\Sounds\\Rotate.wav"));
            sounds.Add("Special", await installedLocation.GetFileAsync("Assets\\Sounds\\Special.mp3"));

            StartGame();
        }

        private void StartGame()
        {
            game = new Game(Canvas.Width, Canvas.Height)
            {
                OnDroppingShape = () => PlaySound("Drop"),
                OnRemovingLines = () => PlaySound("Line"),
                OnBlocked = () => PlaySound("Blocked"),
                OnMove = () => PlaySound("Move"),
                OnRotate = () => PlaySound("Rotate"),
                OnLevelUp = () => 
                    Dispatcher.RunAsync(CoreDispatcherPriority.Normal, () =>
                    {
                        Level.Text = "Level " + game.Level;
                        if (game.SpecialLevel)
                            PlaySound("Special");
                        else
                            PlaySound("LevelUp");
                    }),
                OnGameOver = () =>
                    Dispatcher.RunAsync(CoreDispatcherPriority.Normal, () =>
                    {
                        GameOver.Visibility = Visibility.Visible;
                        PlaySound("GameOver");
                    })
            };
        }

        private void PlaySound(string soundKey)
        {
            if (!sounds.ContainsKey(soundKey))
                return;

            Dispatcher.RunAsync(CoreDispatcherPriority.Normal, async () =>
            {
                if (specialSound.CurrentState == MediaElementState.Playing)
                    return;

                var storageFile = sounds[soundKey];
                var stream = await storageFile.OpenAsync(FileAccessMode.Read);
                var sound = soundKey == "Special" ? specialSound : new MediaElement();
                sound.SetSource(stream, storageFile.ContentType);
                sound.Play();
            });
        }

        private void Canvas_Update(ICanvasAnimatedControl sender, CanvasAnimatedUpdateEventArgs args)
        {
            if(game.GameOver)
                return;

            game.Update(args.Timing, keyboardState);
            keyboardState = new KeyboardState { DownPressed = downPressed };

            Dispatcher.RunAsync(CoreDispatcherPriority.Normal, () =>
            {
                Score.Text = game.Score  + " points";
                LineCount.Text = game.Lines == 1 ? "1 line" : game.Lines + " lines";
            });
        }

        private void Canvas_Draw(ICanvasAnimatedControl sender, CanvasAnimatedDrawEventArgs args)
        {
            game.Draw(args.DrawingSession);
        }

        private void Button_Click(object sender, RoutedEventArgs e)
        {
            if (sender == LeftButton)
                keyboardState.LeftPressed = true;
            else if (sender == RotateButton)
                keyboardState.RotatePressed = true;
            else if (sender == RightButton)
                keyboardState.RightPressed = true;
        }

        private void Down_Pressed(object sender, PointerRoutedEventArgs e)
        {
            downPressed = true;
        }

        private void Down_Released(object sender, PointerRoutedEventArgs e)
        {
            downPressed = false;
        }

        private void Restart_Click(object sender, RoutedEventArgs e)
        {
            GameOver.Visibility = Visibility.Collapsed;
            StartGame();

            Level.Text = "Level 1";
            Score.Text = "0 points";
            LineCount.Text = "0 lines";
        }
    }
}
