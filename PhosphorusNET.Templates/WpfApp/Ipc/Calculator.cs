using System.Threading.Tasks;
using PhosphorusNET.Ipc;

namespace WpfApp.Ipc;


[IpcExpose]
public class Calculator
{
    public int Add(int a, int b)
    {
        return a + b;
    }
    
    public async Task<int> AddAsync(int a, int b)
    {
        await Task.Delay(1000);
        return a + b;
    }
}