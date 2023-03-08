<Project Sdk="Microsoft.NET.Sdk">
  <PropertyGroup>
    <VishnuRoot>$(ProjectDir)../../..</VishnuRoot>
    <AssemblyRoot>$(VishnuRoot)/ReadyBin/Assemblies</AssemblyRoot>
  </PropertyGroup>
  <PropertyGroup>
    <OutputType>Library</OutputType>
    <RootNamespace>NetEti.FileTools.Zip</RootNamespace>
    <AssemblyName>NetEti.ZipAccess</AssemblyName>
  </PropertyGroup>
  <PropertyGroup Condition=" '$(Configuration)|$(Platform)' == 'Debug|AnyCPU' ">
    <DocumentationFile>bin\Debug\NetEti.ZipAccess.xml</DocumentationFile>
  </PropertyGroup>
  <PropertyGroup>
    <TargetFramework>net7.0</TargetFramework>
    <GenerateAssemblyInfo>false</GenerateAssemblyInfo>
  </PropertyGroup>
  <ItemGroup>
    <Reference Include="ICSharpCode.SharpZipLib, Version=2.0.0.1, Culture=neutral, PublicKeyToken=0632af5f2e8db57f, processorArchitecture=MSIL">
      <SpecificVersion>False</SpecificVersion>
      <HintPath>Assemblies\ICSharpCode.SharpZipLib.dll</HintPath>
    </Reference>
    <Reference Include="NetEti.Global">
      <HintPath>$(AssemblyRoot)\NetEti.Global.dll</HintPath>
    </Reference>
    <Reference Include="SevenZipSharp">
      <HintPath>Assemblies\SevenZipSharp.dll</HintPath>
    </Reference>
  </ItemGroup>
  <ItemGroup>
    <PackageReference Include="System.Data.DataSetExtensions" Version="4.5.0" />
    <PackageReference Include="Microsoft.DotNet.UpgradeAssistant.Extensions.Default.Analyzers" Version="0.4.410601">
      <PrivateAssets>all</PrivateAssets>
    </PackageReference>
  </ItemGroup>
  <ItemGroup>
    <FrameworkReference Include="Microsoft.AspNetCore.App" />
  </ItemGroup>
</Project>