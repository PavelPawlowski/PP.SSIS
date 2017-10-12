#Load System.EnterpriseServices assembly as it contain classes to handle GAC
[Reflection.Assembly]::LoadWithPartialName("System.EnterpriseServices")
 
#Create instance of Publish class which can handle GAC Installation and/or removal
[System.EnterpriseServices.Internal.Publish] $publish = new-object System.EnterpriseServices.Internal.Publish;

#Remove from GAC using GacRemove method (Provide full path to the assembly in GAC)
$publish.GacRemove("C:\Windows\Microsoft.NET\assembly\GAC_MSIL\PP.SSIS.DataFlow\v4.0_1.0.0.0__6926746b040a83a5\PP.SSIS.DataFlow.dll")
 
#Install dll into GAC using GacInstall method (Provide full path to the assembly)
$publish.GacInstall("C:\Program Files (x86)\Microsoft SQL Server\110\DTS\PipelineComponents\PP.SSIS.DataFlow.dll");
