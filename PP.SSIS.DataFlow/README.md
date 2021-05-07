# PP.SSIS.DataFlow

**Contains following SSIS Data Flow Componets**

* Hash Column Transformation (Enhanced)
* Columns To Xml Transformation (Enhanced)
* RegEx Extraction Transformation (Enhanced)
* Row Number Transformation
* History Lookup Transformation (new)
* Lookup Error Aggregation Transformation (new)

## Deployment

Run **`PP.SSIS.DataFlow.Deploy.bat`** from elevated command prompt.

The bat file allows you to deploy all or specific version of SSIS components. It copies the libraries and xml mappings to appropriate folders as well as registers the libraries in GAC.

### PP.SSIS.DataFlow.Deploy.bat

**Usage:**

`PP.SSIS.DataFlow.Deploy.bat versionToDeploy gacUtilLocation`

**Examples:**
```bat
PP.SSIS.DataFlow.Deploy.bat all                           (Install all versions of components, detect gacutil)
PP.SSIS.DataFlow.Deploy.bat 2012 "C:\tools\gacutil.exe"   (Install 2012 versions of components, use gacutil provided)
PP.SSIS.DataFlow.Deploy.bat 2012                          (Install 2012 versions of components, detect gacutil)
```

If `gacUtilLocation` is not provided the batch file tries to locate the gacutil. It tries to locate the utility under `C:\Program Files (x86)\Microsoft SDKs\Windows` subfolders.