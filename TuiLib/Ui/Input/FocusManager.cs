using System.Collections.Generic;
using System.Linq;

namespace TuiLib.Ui.Input;

public sealed class FocusManager
{

    private Element _scope;

    public Element? FocusedElement { get; private set; }

    public Element Scope
    {
        get => _scope;
        set
        {
            _scope = value;
            Validate();
        }
    }

    public FocusManager(Element root)
    {
        _scope = root;
    }

    public bool Focus(Element? element)
    {
        if (element != null && !CanFocus(element))
        {
            return false;
        }

        if (ReferenceEquals(element, FocusedElement))
        {
            return true;
        }

        Element? previous = FocusedElement;
        FocusedElement = element;
        previous?.SetFocused(false);
        element?.SetFocused(true);
        return true;
    }

    public void MoveFocus(bool backwards)
    {
        List<Element> candidates = _scope.GetSelfAndDescendants().Where(CanFocus).ToList();
        if (candidates.Count == 0)
        {
            return;
        }

        int index = FocusedElement != null ? candidates.IndexOf(FocusedElement) : -1;
        int next = backwards
            ? (index <= 0 ? candidates.Count - 1 : index - 1)
            : (index + 1) % candidates.Count;
        Focus(candidates[next]);
    }

    public void FocusFirst()
    {
        if (FocusedElement != null && CanFocus(FocusedElement))
        {
            return;
        }

        Element? first = _scope.GetSelfAndDescendants().FirstOrDefault(CanFocus);
        Focus(first);
    }

    internal void Validate()
    {
        if (FocusedElement == null || CanFocus(FocusedElement))
        {
            return;
        }

        Element? previous = FocusedElement;
        FocusedElement = null;
        previous.SetFocused(false);
    }

    public bool CanFocus(Element element)
    {
        if (!element.Focusable || !element.IsEffectivelyEnabled)
        {
            return false;
        }

        for (Element? current = element; current != null; current = current.Parent)
        {
            if (!current.IsVisible)
            {
                return false;
            }

            if (ReferenceEquals(current, _scope))
            {
                return true;
            }
        }

        return false;
    }
}
