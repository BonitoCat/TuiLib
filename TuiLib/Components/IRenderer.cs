using System.Collections.Generic;

namespace TuiLib.Components;

public interface IRenderer
{
    public void Render(IEnumerable<RenderPart> parts);
}