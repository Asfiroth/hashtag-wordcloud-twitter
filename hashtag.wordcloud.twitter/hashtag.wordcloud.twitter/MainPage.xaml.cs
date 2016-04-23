using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.IO;
using System.Linq;
using System.Net.NetworkInformation;
using System.Runtime.InteropServices.WindowsRuntime;
using Windows.Foundation;
using Windows.Foundation.Collections;
using Windows.System.Threading;
using Windows.UI.Core;
using Windows.UI.Popups;
using Windows.UI.Xaml;
using Windows.UI.Xaml.Controls;
using Windows.UI.Xaml.Controls.Primitives;
using Windows.UI.Xaml.Data;
using Windows.UI.Xaml.Input;
using Windows.UI.Xaml.Media;
using Windows.UI.Xaml.Media.Animation;
using Windows.UI.Xaml.Media.Imaging;
using Windows.UI.Xaml.Navigation;
using hashtag.wordcloud.twitter.Models;
using LinqToTwitter;

// The Blank Page item template is documented at http://go.microsoft.com/fwlink/?LinkId=402352&clcid=0x409

namespace hashtag.wordcloud.twitter
{
    /// <summary>
    /// An empty page that can be used on its own or navigated to within a Frame.
    /// </summary>
    public sealed partial class MainPage : Page
    {
        private ListBoxItem _item;
        public MainPage()
        {
            this.InitializeComponent();
            Loaded += MainPage_Loaded;
        }

        private void MainPage_Loaded(object sender, RoutedEventArgs e)
        {
            Start();
        }

        private void Start()
        {
            if (!NetworkInterface.GetIsNetworkAvailable())
            {
                new MessageDialog("No hay internet!", "HashTag").ShowAsync();
                return;
            }
            var tweets = GetTweets();
            var tagItems = tweets as TagItem[] ?? tweets.ToArray();
            TagBox.ItemsSource =
                new ObservableCollection<TagItem>(tagItems);
            var startTime = DateTime.Now;

            var timeSpan = TimeSpan.FromSeconds(10);
            var panelTimer = ThreadPoolTimer.CreatePeriodicTimer((response) =>
            {
                var rand = new Random();

                Dispatcher.RunAsync(CoreDispatcherPriority.High, () =>
                {
                    if (DateTime.Now >= startTime.AddMinutes(3))
                    {
                        var newTweets = GetTweets();
                        var newTagItems = newTweets as TagItem[] ?? newTweets.ToArray();
                        TagBox.ItemsSource = new ObservableCollection<TagItem>(newTagItems);
                        startTime = DateTime.Now;
                    }
                    var tag = (ListBoxItem)TagBox.ItemContainerGenerator.ContainerFromIndex(rand.Next(0, tagItems.Length));
                    if (tag == null) return;
                    ZoomOutPanel();
                    _item = tag;
                });
            }, timeSpan);
        }

        private IEnumerable<TagItem> GetTweets()
        {
            var auth = new SingleUserAuthorizer
            {
                CredentialStore = new InMemoryCredentialStore
                {
                    ConsumerKey = Constants.ConsumerKey,
                    ConsumerSecret = Constants.ConsumerSecret,
                    OAuthToken = Constants.AccessToken,
                    OAuthTokenSecret = Constants.AccessTokenSecret
                }
            };

            var context = new TwitterContext(auth);
            var searchResults =
                             (from search in context.Search
                              where search.Type == SearchType.Search &&
                                    search.Query == Constants.Query &&
                                    search.Count == 100 &&
                                    search.ResultType == ResultType.Recent &&
                                    search.IncludeEntities == true
                              select search).SingleOrDefault();

            var rd = new Random();
            return searchResults.Statuses.OrderByDescending(x => x.CreatedAt).Take(50).Select(
                x =>
                    new TagItem
                    {
                        Name = x.User.Name,
                        Weight = rd.Next(10, 50),
                        Tweet =
                            new Tweet
                            {
                                Description = x.Text,
                                UserName = x.User.Name,
                                UserFotoUrl = x.User.ProfileImageUrl
                            }
                    });
        }

        private void ZoomOutPanel()
        {
            var rnd = new Random();
            var sb = new Storyboard();
            var propertyX = new PropertyPath("(UIElement.RenderTransform).(CompositeTransform.ScaleX)");
            var propertyY = new PropertyPath("(UIElement.RenderTransform).(CompositeTransform.ScaleY)");
            var t = (TweetPanel.RenderTransform as CompositeTransform) ?? new CompositeTransform();
            TweetPanel.RenderTransform = t;

            var xAnim = new DoubleAnimation()
            {
                BeginTime = TimeSpan.FromMilliseconds(0),
                From = 1f,
                To = 0,
                Duration = TimeSpan.FromMilliseconds(1000),
            };
            Storyboard.SetTarget(xAnim, TweetPanel);
            Storyboard.SetTargetProperty(xAnim, propertyX.Path);
            sb.Children.Add(xAnim);

            var yAnim = new DoubleAnimation
            {
                BeginTime = TimeSpan.FromMilliseconds(0),
                From = 1f,
                To = 0,
                Duration = TimeSpan.FromMilliseconds(1000),
            };

            Storyboard.SetTarget(yAnim, TweetPanel);
            Storyboard.SetTargetProperty(yAnim, propertyY.Path);
            sb.Children.Add(yAnim);
            sb.Begin();
            sb.Completed += Sb_Completed;
        }
        private void ZoomInPanel()
        {
            var rnd = new Random();
            var sb = new Storyboard();
            var propertyX = new PropertyPath("(UIElement.RenderTransform).(CompositeTransform.ScaleX)");
            var propertyY = new PropertyPath("(UIElement.RenderTransform).(CompositeTransform.ScaleY)");
            var t = (TweetPanel.RenderTransform as CompositeTransform) ?? new CompositeTransform();
            TweetPanel.RenderTransform = t;
            t.TranslateX = rnd.NextDouble() * 100 - 50;
            t.TranslateY = rnd.NextDouble() * 100 - 50;

            var xAnim = new DoubleAnimation()
            {
                BeginTime = TimeSpan.FromMilliseconds(0),
                To = 1f,
                From = 0,
                Duration = TimeSpan.FromMilliseconds(1000),
            };
            Storyboard.SetTarget(xAnim, TweetPanel);
            Storyboard.SetTargetProperty(xAnim, propertyX.Path);
            sb.Children.Add(xAnim);

            var yAnim = new DoubleAnimation
            {
                BeginTime = TimeSpan.FromMilliseconds(0),
                To = 1f,
                From = 0,
                Duration = TimeSpan.FromMilliseconds(1000),
            };

            Storyboard.SetTarget(yAnim, TweetPanel);
            Storyboard.SetTargetProperty(yAnim, propertyY.Path);
            sb.Children.Add(yAnim);
            sb.Begin();
        }

        private void Sb_Completed(object sender, object e)
        {
            TweetPanel.Visibility = Visibility.Visible;
            _item.IsSelected = true;
            ZoomInPanel();
        }

        private void TagBox_OnSelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            if (e.AddedItems.Count == 0) return;
            var tagItem = e.AddedItems[0] as TagItem;
            if (tagItem == null) return;
            var tweet = tagItem.Tweet;
            UserFoto.ImageSource = new BitmapImage(new Uri(tweet.UserFotoUrl, UriKind.Absolute));
            UserName.Text = tweet.UserName;
            UserTweet.Text = tweet.Description;
        }
    }
}
