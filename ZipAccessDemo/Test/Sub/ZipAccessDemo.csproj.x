<Project Sdk="Microsoft.NET.Sdk">
  <PropertyGroup>
    <TargetFramework>net7.0-windows</TargetFramework>
    <VishnuRoot>$(ProjectDir)../../..</VishnuRoot>
    <AssemblyRoot>$(VishnuRoot)/ReadyBin/Assemblies</AssemblyRoot>
    <GenerateAssemblyInfo>false</GenerateAssemblyInfo>
    <UseWindowsForms>true</UseWindowsForms>
    <ImportWindowsDesktopTargets>true</ImportWindowsDesktopTargets>
  </PropertyGroup>
  <PropertyGroup>
    <OutputType>WinExe</OutputType>
    <RootNamespace>NetEti.DemoApplications</RootNamespace>
  </PropertyGroup>
  <PropertyGroup Condition=" '$(Configuration)|$(Platform)' == 'Debug' ">
    <Optimize>false</Optimize>
    <WarningLevel>4</WarningLevel>
  </PropertyGroup>
  <PropertyGroup Condition=" '$(Configuration)|$(Platform)' == 'Release' ">
    <Optimize>true</Optimize>
    <WarningLevel>4</WarningLevel>
  </PropertyGroup>
  <PropertyGroup Condition="'$(Configuration)|$(Platform)' == 'Debug|AnyCPU'">
    <DocumentationFile>bin\Debug\ZipAccessDemo.XML</DocumentationFile>
  </PropertyGroup>
  <ItemGroup>
    <None Update="Test\Debug.zip">
      <CopyToOutputDirectory>Always</CopyToOutputDirectory>
    </None>
  </ItemGroup>
  <ItemGroup>
    <ProjectReference Include="..\ZipAccess\ZipAccess.csproj" />
  </ItemGroup>
  <ItemGroup>
    <Content Include="7z32.dll">
      <CopyToOutputDirectory>Always</CopyToOutputDirectory>
    </Content>
    <Content Include="7z64.dll">
      <CopyToOutputDirectory>Always</CopyToOutputDirectory>
    </Content>
  </ItemGroup>
  <ItemGroup>
    <PackageReference Include="Microsoft.CSharp" Version="4.7.0" />
    <PackageReference Include="System.Data.DataSetExtensions" Version="4.5.0" />
    <PackageReference Include="Microsoft.DotNet.UpgradeAssistant.Extensions.Default.Analyzers" Version="0.4.410601">
      <PrivateAssets>all</PrivateAssets>
    </PackageReference>
    <PackageReference Include="Microsoft.Windows.Compatibility" Version="7.0.0" />
  </ItemGroup>
</Project>