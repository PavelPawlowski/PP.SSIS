# PP.SSIS.ControlFlow

**Contains following SSIS Control Flow Componets**

* Sleep Task (SSIS 2012+)
* Wait For File Task
* Wait for Sinal Task
* Wait for Time Task
* Variables to Xml Task

## Deployment

Run **`PP.SSIS.ControlFlow.Deploy.bat`** from elevated command prompt.

The bat file allows you to deploy all or specific version of SSIS components. It copies the libraries and xml mappings to appropriate folders as well as registers the libraries in GAC.

### PP.SSIS.ControlFlow.Deploy.bat

**Usage:**

`PP.SSIS.ControlFlow.Deploy.bat versionToDeploy gacUtilLocation`

**Examples:**
```bat
PP.SSIS.ControlFlow.Deploy.bat all                           (Install all versions of components, detect gacutil)
PP.SSIS.ControlFlow.Deploy.bat 2012 "C:\tools\gacutil.exe"   (Install 2012 versions of components, use gacutil provided)
PP.SSIS.ControlFlow.Deploy.bat 2012                          (Install 2012 versions of components, detect gacutil)
```

If `gacUtilLocation` is not provided the batch file tries to locate the gacutil. It tries to locate the utility under `C:\Program Files (x86)\Microsoft SDKs\Windows` subfolders.