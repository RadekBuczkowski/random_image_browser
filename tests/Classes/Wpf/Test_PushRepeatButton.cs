namespace Random_Image_Tests.Classes.Wpf;

using System.Collections.Generic;
using System.Threading.Tasks;

using Random_Image.Classes.Utilities;
using Random_Image.Classes.Wpf;

public class Test_PushRepeatButton
{
    [Fact]
    public async Task Test_Push()
    {
        // arrange
        List<bool> actual_result = new();

        // act
        await MainThread.InvokeSTA(() =>
        {
            PushRepeatButton sut = new();
            actual_result.Add(sut.IsPushed == false);
            sut.Push();
            actual_result.Add(sut.IsPushed == true);
        });

        // assert
        Assert.DoesNotContain(false, actual_result);
    }
}
