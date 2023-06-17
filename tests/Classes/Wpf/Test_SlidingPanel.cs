namespace Random_Image_Tests.Classes.Wpf;

using System.Threading.Tasks;

using Random_Image.Classes.Utilities;
using Random_Image.Classes.Wpf;

public class Test_SlidingPanel
{
    [Fact]
    public async Task Test_Twin_Expand()
    {
        // arrange
        List<bool> actual_result = new();

        // act
        await MainThread.InvokeSTA(() =>
        {
            SlidingPanel sut1 = new();
            SlidingPanel sut2 = new();
            sut1.Twin = sut2;
            actual_result.Add(sut1.IsExpanded == false);
            actual_result.Add(sut2.IsExpanded == false);
            sut1.Expand();
            actual_result.Add(sut1.IsExpanded == true);
            actual_result.Add(sut2.IsExpanded == true);
        });

        // assert
        Assert.DoesNotContain(false, actual_result);
    }
}
