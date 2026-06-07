using System.Collections.Generic;
using System.Threading.Tasks;

namespace GCMod.Interfaces;

public interface ITranslationProvider
{
    Dictionary<string, string> Names { get; }
    Dictionary<string, string> Words { get; }
    Dictionary<int, Dictionary<string, string>> Novels { get; }

    void Initialize();
    Task GetNovelTranslationAsync(int novelId);
}
