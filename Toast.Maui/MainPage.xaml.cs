namespace Toast.Maui
{
    public partial class MainPage : ContentPage
    {
        private MainPageViewModel mainPageViewModel;

        public MainPage(MainPageViewModel viewModel)
        {
            InitializeComponent();
            mainPageViewModel = viewModel;
        }

        protected override void OnAppearing()
        {
            base.OnAppearing();
            mainPageViewModel._naviagtion = this.Navigation;
            BindingContext = mainPageViewModel;
        }
    }
}