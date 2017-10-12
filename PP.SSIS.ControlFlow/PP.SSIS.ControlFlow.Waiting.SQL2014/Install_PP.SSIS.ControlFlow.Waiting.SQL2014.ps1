#Load System.EnterpriseServices assembly as it contain classes to handle GAC
[Reflection.Assembly]::LoadWithPartialName("System.EnterpriseServices")
 
#Create instance of Publish class which can handle GAC Installation and/or removal
[System.EnterpriseServices.Internal.Publish] $publish = new-object System.EnterpriseServices.Internal.Publish;

#Remove from GAC using GacRemove method (Provide full path to the assembly in GAC)
$publish.GacRemove("C:\Windows\Microsoft.NET\assembly\GAC_MSIL\PP.SSIS.ControlFlow.Waiting\v4.0_1.0.0.0__d958e388b0ffd524\PP.SSIS.ControlFlow.Waiting.dll")
 
#Install dll into GAC using GacInstall method (Provide full path to the assembly)
$publish.GacInstall("C:\Program Files (x86)\Microsoft SQL Server\120\DTS\Tasks\PP.SSIS.ControlFlow.Waiting.dll");
