## obj-and-bin-delete-folder
This program clears bin and obg folders from project directory.
dotnet publish -r win-x64 -c Release --self-contained -o release /p:PublishSingleFile=true /p:PublishTrimmed=true

#### Fast start
Just enter in the Clean.setting rows with full address to directories

Example: 
```json  
  "Directories": {
      "1": "D:\\Test 1",
      "2": "D:\\SomeDirectory\\Temp"
    }
```

#### To Use Automatic clean:
    "AutoClean": true

#### Local clean
    "CleanLocal": true
#### to automatic clean directories in setting
    "AutoClean": true,
    "CleanLocal": false

## If you use single file publish
Settings will work in auto-local clean

But you can use command line arguments:

-l : Set automatical local clean

-d D:\Temp ; D:\SomeDirectory  : Set automatical clean two directoryes split by ';'

-a : Ask user what directory need to clean;
