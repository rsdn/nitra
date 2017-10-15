using Nitra.ClientServer.Messages;
using System.Diagnostics;
using System.Text.RegularExpressions;
using System.Windows.Media;

namespace Nitra.Visualizer.ViewModels
{
  public class PropertyAstNodeViewModel : AstNodeViewModel
  {
    readonly PropertyDescriptor _propertyDescriptor;
    readonly PropertyDescriptor _assignLocation;

    public PropertyAstNodeViewModel(AstContext context, PropertyDescriptor propertyDescriptor, PropertyDescriptor assignLocation)
      : this(context, propertyDescriptor)
    {
      _assignLocation = assignLocation;
    }

    public PropertyAstNodeViewModel(AstContext context, PropertyDescriptor propertyDescriptor)
      : base(context, propertyDescriptor.Object)
    {
      _propertyDescriptor = propertyDescriptor;
    }

    public string Name
    {
      get { return _propertyDescriptor.Name; }
    }

    public string Pefix
    {
      get
      {
        switch (_propertyDescriptor.Kind)
        {
          case PropertyKind.Ast:            return "ast ";
          case PropertyKind.DependentIn:    return "in ";
          case PropertyKind.DependentInOut: return "inout ";
          case PropertyKind.DependentOut:   return "out ";
          default:                          return "";
        }
      }
    }

    public Brush Foreground
    {
      get
      {
        switch (_propertyDescriptor.Kind)
        {
          case PropertyKind.Ast:            return Brushes.DarkGoldenrod;
          case PropertyKind.DependentIn:
          case PropertyKind.DependentInOut:
          case PropertyKind.DependentOut:   return Brushes.Green;
          default:                          return Brushes.DarkGray;
        }
      }
    }

    public (string Path, int Line, int Column) AssignLocation
    {
      get
      {
        try
        {
          if (_assignLocation != null && _assignLocation.Object.Value is string text)
          {
            var rx = new Regex(@"\((.*), (\d+), (\d+)\)");
            var res = rx.Match(text);
            if (res.Success)
              return (res.Groups[1].Value, int.Parse(res.Groups[2].Value), int.Parse(res.Groups[3].Value));
          }
        }
        catch { }

        return (null, 0, 0);
      }
    }

    public bool IsAssignLocationAvalable => !string.IsNullOrEmpty(AssignLocation.Path);

    public string AssignLocationText
    {
      get
      {
        var (path, line, col) = AssignLocation;

        if (string.IsNullOrEmpty(path))
          return "No source code avalable";

        return $"{path}:({line}, {col})";
      }
    }

    public override string ToString()
    {
      return _propertyDescriptor.ToString();
    }
  }
}
