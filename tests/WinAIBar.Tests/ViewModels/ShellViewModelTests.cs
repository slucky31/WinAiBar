using NSubstitute;
using WinAIBar.Core.Services.Navigation;
using WinAIBar.Core.ViewModels;
using Xunit;

namespace WinAIBar.Tests.ViewModels;

public sealed class ShellViewModelTests
{
    [Fact]
    public void NavigateCommandExecutesNavigationService()
    {
        _ = TestContext.Current.CancellationToken;
        var navigationService = Substitute.For<INavigationService>();
        var viewModel = new ShellViewModel(navigationService);

        viewModel.NavigateCommand.Execute("Claude");

        navigationService.Received(1).NavigateTo("Claude");
    }

    [Fact]
    public void SelectedItemSetRaisesPropertyChanged()
    {
        _ = TestContext.Current.CancellationToken;
        var navigationService = Substitute.For<INavigationService>();
        var viewModel = new ShellViewModel(navigationService);
        var raised = false;
        viewModel.PropertyChanged += (_, e) =>
        {
            if (e.PropertyName == nameof(ShellViewModel.SelectedItem))
                raised = true;
        };

        viewModel.SelectedItem = new object();

        Assert.True(raised);
    }

    [Fact]
    public void ConstructorNullNavigationServiceThrowsArgumentNullException()
    {
        _ = TestContext.Current.CancellationToken;
        Assert.Throws<ArgumentNullException>(() => new ShellViewModel(null!));
    }
}
