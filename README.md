# Queqiao Matchmaking Library

## What is it?
Queqiao Matchmaking Library is a library built for C#, using Sockets and TcpClient/TcpListener as its framework.

## How to use it?
Download the directory `publish`.

### Visual Studio
Check [this](https://learn.microsoft.com/en-us/dotnet/core/tutorials/library-with-visual-studio#add-a-project-reference) official tutorial.

### Others
Find the project file `.csproj`. Edit it as follows:<br>
```csproj
<ItemGroup>
  <Reference Include="Queqiao">
    <HintPath>path\to\the\library\publish\Queqiao.dll</HintPath>
  </Reference>
</ItemGroup>
```

Then, type the following at the beginning of your project:<br>
```cs
using Queqiao;
```

Now, you can use the library freely!
