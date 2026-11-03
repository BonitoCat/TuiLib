using System;
using System.Collections.Generic;
using System.Text;
using InputLib;
using InputLib.EventArgs;
using TuiLib.Components;

namespace TuiLib.Controls;

public class TuiTextInput : Control
{
    public override ControlType Type => ControlType.TextInput;
    public TuiLabel Label { get; set; } = new();
    
    public string Content
    {
        get => _inputBuilder.ToString();
        set => _inputBuilder = new(value);
    }
    
    /// <summary>
    /// Flags if null should be returns if enter is pressed without anything inputed
    /// </summary>
    public bool EnterReturnsNull { get; set; } = false;
    
    /// <summary>
    /// Flags if 'n' should be returned when escaping out of input
    /// </summary>
    public bool EscapeReturnsNo { get; set; } = false;
    
    /// <summary>
    /// Flags if the input should be hidden (e.g. password input)
    /// </summary>
    public bool IsInputHidden { get; set; } = false;
    
    /// <summary>
    /// The char to show instead of the input
    /// </summary>
    public char HideChar { get; set; } = '*';
    
    private StringBuilder _inputBuilder = new();

    private int _cursorPos;
    
    public string? ReadInput()
    {
        return null;
        
        if (System.Type.GetType("InputLib.InputListener") == null)
        {
            string? input = Console.ReadLine();
            if (EnterReturnsNull)
            {
                return input;
            }

            return input ?? "";
        }

        InputListener? keyListener = InputListener.New();
        if (keyListener == null)
        {
            return "";
        }
        
        keyListener.StartListening();
        _inputBuilder = new(Content);
        string? result = null;

        _cursorPos = _inputBuilder.Length;
        void Redraw()
        {
            // yet to implement
        }
        
        void OnKeyDown(Key key, KeyDownEventArgs e)
        {
            if (key == Key.Enter)
            {
                result = EnterReturnsNull && _inputBuilder.Length == 0
                    ? null
                    : _inputBuilder.ToString();

                keyListener.Dispose();
                return;
            }

            if (key == Key.ArrowLeft)
            {
                if (_cursorPos > 0)
                {
                    _cursorPos--;
                    Redraw();
                }
                return;
            }

            if (key == Key.ArrowRight)
            {
                if (_cursorPos < _inputBuilder.Length)
                {
                    _cursorPos++;
                    Redraw();
                }
                return;
            }

            if (key == Key.Backspace)
            {
                if (_cursorPos > 0)
                {
                    _inputBuilder.Remove(_cursorPos - 1, 1);
                    _cursorPos--;
                    Redraw();
                }
                return;
            }

            if (key == Key.Delete)
            {
                if (_cursorPos < _inputBuilder.Length)
                {
                    _inputBuilder.Remove(_cursorPos, 1);
                    Redraw();
                }
                return;
            }

            char c = key.Char;
            if (c != '\0')
            {
                _inputBuilder.Insert(_cursorPos, c);
                _cursorPos++;
                Redraw();
            }
        }
        
        void OnKeyUp(Key key, KeyUpEventArgs e)
        {
            if (key == Key.Escape)
            {
                result = EscapeReturnsNo ? "n" : null;
                keyListener.Dispose();
            }
        }
        
        /*keyListener.OnKeyDown += OnKeyDown;
        keyListener.OnKeyUp += OnKeyUp;

        Redraw();
        
        keyListener.WaitForDispose();
        keyListener.OnKeyDown -= OnKeyDown;
        keyListener.OnKeyUp -= OnKeyUp;*/
        
        return result;
    }

    protected override IEnumerable<RenderPart> RenderCore()
    {
        yield return new()
        {
            X = X,
            Y = Y,
            Text = ToString(),
        };
    }

    public string GetInput()
    {
        return _inputBuilder.ToString();
    }

    public override string ToString()
    {
        StringBuilder builder = new();
        builder.Append(Label);
        
        if (!IsInputHidden)
        {
            builder.Append(_inputBuilder);
        }
        else
        {
            builder.Append(new string(HideChar, _inputBuilder.Length));
        }

        if (Parent == null)
        {
            return builder.ToString();
        }

        int labelLength = Label != null ? Color.TrimAnsi(Label.ToString()).Length : 0;
        if (labelLength + _inputBuilder.Length > Parent.MaxWidth)
        {
            
        }
        
        return builder.ToString();
    }
}