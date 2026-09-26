using ShoppetApp.ViewModels;

namespace ShoppetApp.Pages
{
    public partial class PostSettingsPage : ContentPage
    {
        private readonly PostSettingsViewModel _vm;
        public PostSettingsPage(PostSettingsViewModel vm)
        {
            InitializeComponent();
            BindingContext = _vm = vm;
        }

        protected override async void OnAppearing()
        {
            base.OnAppearing();
            await _vm.LoadMyPostsAsync();
        }
    }
}
