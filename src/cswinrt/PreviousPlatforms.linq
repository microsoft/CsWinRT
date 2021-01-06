<Query Kind="Statements" />

// This script can be run periodically from within LINQPad (https://www.linqpad.net) 
// to generate aggregate data for get_contract_platform in helpers.h

var doc = XDocument.Load(@"C:\Program Files (x86)\Windows Kits\10\Platforms\UAP\10.0.19041.0\PreviousPlatforms.xml");

XNamespace pp = "http://microsoft.com/schemas/Windows/SDK/PreviousPlatforms";

var contracts =
	from platform in doc.Elements().Elements()
	let PlatformName = platform.Attribute("friendlyName").Value
	let PlatformVersion = platform.Attribute("version").Value
	from contract in platform.Elements(pp + "ContainedApiContracts").Elements()
	let ContractName = contract.Attribute("name").Value
	let ContractVersion = contract.Attribute("version").Value
	let ContractNumber = int.Parse(ContractVersion.Replace(".0.0.0",""))
	orderby ContractName, ContractNumber, PlatformVersion
	group new{ContractNumber, PlatformVersion} by ContractName;

foreach(var contract in contracts)
{
	String.Format(
@"            {{ ""{0}"",
                {{", contract.Key).Dump();
	int ContractNumber = 0;
	foreach(var mapping in contract)
	{
		if(mapping.ContractNumber == ContractNumber) continue;
		ContractNumber= mapping.ContractNumber;
		String.Format(
@"                    {{ {0}, ""{1}"" }},", mapping.ContractNumber, mapping.PlatformVersion).Dump();
	}
	
@"                }
            },".Dump();
}

