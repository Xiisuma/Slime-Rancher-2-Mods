using System.Reflection;
using System.Runtime.InteropServices;
using MelonLoader;
using MelonSRML;

[assembly: AssemblyTitle("SlimePondUpgradesSR2")]
[assembly: AssemblyDescription("Allows you to upgrade the Pond — SR2 port")]
[assembly: AssemblyCompany("")]
[assembly: AssemblyProduct("SlimePondUpgradesSR2")]
[assembly: AssemblyCopyright("Copyright © Aidanamite 2021, SR2 port 2026")]
[assembly: AssemblyTrademark("")]
[assembly: AssemblyCulture("")]
[assembly: ComVisible(false)]
[assembly: Guid("a1b2c3d4-e5f6-4a7b-8c9d-0e1f2a3b4c5d")]
[assembly: AssemblyVersion("1.0.0.0")]
[assembly: AssemblyFileVersion("1.0.0.0")]

// Tell MelonLoader this is a mod for Slime Rancher 2
[assembly: MelonInfo(typeof(PondUpgrades.Main), "Pond Upgrades SR2", "1.0.0", "Aidanamite")]
[assembly: MelonColor(ConsoleColor.Cyan)]
[assembly: MelonGame("MonomiPark", "SlimeRancher2")]
