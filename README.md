# C# Mono.Cecil IL Code Weaver INotifyPropertyChanged-Example

## What's going on here?

In C# a basic class implementation may look like this:

```csharp
public class ViewModel
{
    public int SomeValue { get; set; }
}
```

With WPF MVVM the basic implementation of a ViewModel will look something like this:
```csharp
public class ViewModel : INotifyPropertyChanged
{
    public event PropertyChangedEventHandler PropertyChanged;

    private int _someValue;
    public int SomeValue
    {
        get { return _someValue; }
        set
        {
            if(_someValue != value)
            {
                _someValue = value;
                PropertyChanged?.Invoke(this, new PropertyChangedEventArgs("SomeValue"));
            }
        }
    }
}
```

Well... this is some code to write for such a simple task. The most usual step would be to write an abstract class which minimizes this code (imagine the mess with more properties).

An abstract base ViewModel could look like this:
```csharp
public abstract class BaseViewModel : INotifyPropertyChanged
{
    public event PropertyChangedEventHandler PropertyChanged;

    protected virtual void SetField<T>(
        ref T field, 
        T value, 
        IEqualityComparer<T> comparer = null, 
        [CallerMemberName]string propertyName = null)
    {
        if (comparer == null)
            comparer = EqualityComparer<T>.Default;

        if(!comparer.Equals(field, value))
        {
            field = value;
            OnPropertyChanged(propertyName);
        }
    }

    protected virtual void OnPropertyChanged(string propertyName = null)
    {
        PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
    }
}
```

The same ViewModel which implements the base:

```csharp
public class ViewModel : BaseViewModel
{
    private int _someValue;
    public int SomeValue
    {
        get { return _someValue; }
        set { SetField(ref _someValue, value); }
    }
}
```

Works like a charm and is the easiest way to simplify a ViewModel.

## Nothing new to me...

Wouldn't it be nice if someone could just write the property itself without backing field and setter logic? 

```csharp
public class ViewModel : INotifyPropertyChanged
{
    public event PropertyChangedEventHandler PropertyChanged;
        
    [NotifyPropertyChanged]
    public int SomeValue { get; set; }
}
```

The concept of <abbr title="Aspect Oriented Programming">AOP</abbr> would allow some kind of behavior. Sadly C# does not support AOP directly. And that is what IL Code Weaving is for. With frameworks like Mono.Cecil someone can write IL instructions directly into an assembly.

### What have to be changed so the basic ViewModel implementation looks like the AOP example but with the same behavior?
- A auto property with `{ get; set; }` does actually have a invisible backing field named `<PropertyName>k__BackingField`. So we don't have to actually create that
- The `get` is exactly the same with or without the INotifyPropertyChanged implementation
- So there is just the `set` method left

Looking at the IL Code of the property setter in the basic ViewModel implementation we can see the following (decompiler used in this example [JetBrains dotPeek][1]):

```csharp
.method public hidebysig specialname instance void 
    set_SomeValue(
      int32 'value'
    ) cil managed 
  {
    .maxstack 3
    .locals init (
      [0] bool V_0
    )

    // [27 13 - 27 14]
    IL_0000: nop          

    // [28 17 - 28 41]
    IL_0001: ldarg.0      // this
    IL_0002: ldfld        int32 TestApp.ViewModels3.ViewModel::_someValue
    IL_0007: ldarg.1      // 'value'
    IL_0008: ceq          
    IL_000a: ldc.i4.0     
    IL_000b: ceq          
    IL_000d: stloc.0      // V_0

    IL_000e: ldloc.0      // V_0
    IL_000f: brfalse.s    IL_0037

    // [29 17 - 29 18]
    IL_0011: nop          

    // [30 21 - 30 40]
    IL_0012: ldarg.0      // this
    IL_0013: ldarg.1      // 'value'
    IL_0014: stfld        int32 TestApp.ViewModels3.ViewModel::_someValue

    // [31 21 - 31 94]
    IL_0019: ldarg.0      // this
    IL_001a: ldfld        class [System]System.ComponentModel.PropertyChangedEventHandler TestApp.ViewModels3.ViewModel::PropertyChanged
    IL_001f: dup          
    IL_0020: brtrue.s     IL_0025
    IL_0022: pop          
    IL_0023: br.s         IL_0036
    IL_0025: ldarg.0      // this
    IL_0026: ldstr        "SomeValue"
    IL_002b: newobj       instance void [System]System.ComponentModel.PropertyChangedEventArgs::.ctor(string)
    IL_0030: callvirt     instance void [System]System.ComponentModel.PropertyChangedEventHandler::Invoke(object, class [System]System.ComponentModel.PropertyChangedEventArgs)
    IL_0035: nop          

    // [32 17 - 32 18]
    IL_0036: nop          

    // [33 13 - 33 14]
    IL_0037: ret          

  } // end of method ViewModel::set_SomeValue
```

The standard setter looks like this:

```csharp
.method public hidebysig specialname instance void 
    set_SomeValue(
      int32 'value'
    ) cil managed 
  {
    .custom instance void [mscorlib]System.Runtime.CompilerServices.CompilerGeneratedAttribute::.ctor() 
      = (01 00 00 00 )
    .maxstack 8

    // [14 37 - 14 41]
    IL_0000: ldarg.0      // this
    IL_0001: ldarg.1      // 'value'
    IL_0002: stfld        int32 TestApp.ViewModels.ViewModel2::'<SomeValue>k__BackingField'
    IL_0007: ret          

  } // end of method ViewModel2::set_SomeValue
```

So basically someone have to copy and paste the instructions right in there. This project does exactly this task.

## Get the project (Visual Studio)
1. Clone the repository
    ```
    git clone "https://github.com/Seng3694/IL-Code-Weaver-INotifyPropertyChanged-Example" ILCodeWeaver
    ```

2. Open the `ILCodeWeaving.sln` Solution file
3. Make sure TestApp is the StartUp project
4. Notice the post-build task in the TestApp project. This will execute the Weaver.exe with the TestApp.exe path as an argument
    ```
    "$(SolutionDir)Weaver\bin\$(ConfigurationName)\Weaver.exe" "$(TargetPath)"
    ```
5. Notice the project reference from TestApp to Weaver. This ensures that the Weaver.exe is built before the TestApp will be built
6. Running the TestApp and typing something in the left TextBox should look like this:
![Test Run][2]

[1]:https://www.jetbrains.com/decompiler/
[2]:https://cdn.discordapp.com/attachments/425728769236664350/426434401405108224/unknown.png