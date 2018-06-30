function GetAvailableDriveLetter()
{
	$normalizedName = ls function:[d-z]: -n | ?{ !(test-path $_) } | select -First 1
	$letter = $normalizedName[0]
	return $letter;
}

function GetAvailableSpace()
{
	param($diskNumber) 

	$disk = Get-Disk -Number $diskNumber
	return $($disk.Size) -$($disk.AllocatedSize)
}

function EnsurePartitionsAreMounted() 
{
	EnsurePartitionMountedForVolume 'EFIESP' 'FAT'
	EnsurePartitionMountedForVolume 'MainOS' 'NTFS'
	EnsurePartitionMountedForVolume 'Data' 'NTFS'
}

function EnsureCorrectFilesFolder() 
{
	$paths = 
	(
		"Core\BootShim.efi",
		"Core\UEFI.elf",
		"Drivers",
		"Developer Menu"
	)

	Write-Host "Checking the binary Files folder..."
	
	foreach ($path in $paths) 
	{
		$fullPath = (Join-Path $PSScriptRoot (Join-Path "Files" $path))
		Write-Host "Checking for path" $fullPath
		if (!(Test-Path $fullPath))
		{
			throw "Sanity check: The Files folder required by the script seems to be incomplete. Could not find '$fullPath'. Did you copy 'the Files' under the Installation\Files folder?"
		}
	}	
}

function EnsurePartitionMountedForVolume
{
	param([string]$label,[string]$fileSystemType) 
	Write-Host "Ensuring the required Phone partitions are mounted..."

	$vol = GetVolume $label $fileSystemType
	
	if ($vol -eq $null) 
	{
		throw "Could not get the volume with label $($label)"
	}

	if ($vol.DriveLetter -eq $null) 
	{
		$part = Get-Partition -Volume $vol
		$freeLetter = GetAvailableDriveLetter		
		$part | Set-Partition -NewDriveLetter $freeLetter
	}
}

function GetPhoneDisk()
{
	foreach ($disk in Get-Disk) 
	{
		foreach ($partition in Get-Partition -Disk $disk) 	
		{
			foreach ($volume in Get-Volume -Partition $partition) 	
			{
				if ($volume.FileSystemType -eq 'NTFS' -and $volume.FileSystemLabel -eq 'MainOS') 
				{
					return $disk;						
					break
				}
			}	
		}
	}
	
	throw 'Could not obtain the MainOS Volume. Please, verify that your phone is in Mass Storage Mode'		
}


function GetMainOS()
{
	foreach ($disk in Get-Disk) 
	{
		foreach ($partition in Get-Partition -Disk $disk) 	
		{
			foreach ($volume in Get-Volume -Partition $partition) 	
			{
				if ($volume.FileSystemType -eq 'NTFS' -and $volume.FileSystemLabel -eq 'MainOS') 
				{
					return New-Object PSObject -Property @{           
						Volume  = $volume
						Disk	= $disk        
					}  

					break
				}
			}	
		}
	}
	
	throw 'Could not obtain the MainOS Volume. Please, verify that your phone is in Mass Storage Mode'		
}

function GetVolume() 
{
	param([string]$label,[string]$fileSystemType) 

	$mainOs = GetMainOS
	$disk = $mainOs.Disk
	
	$vol = Get-Partition -Disk $disk | Get-Volume | Where { $_.FileSystemType -eq $fileSystemType -and $_.FileSystemLabel -eq $label } | Select -index 0

	if ($vol -eq $null)	
	{	
		throw "Could not obtain the $($label) volume. Please, verify it is mounted and that your phone is in Mass Storage Mode"	
	}

	return $vol
}

function GetReservedPartition() 
{
	$mainOs = GetMainOS
	$disk = $mainOs.Disk
	
	$vol = $disk | Get-Partition | Where { $_.GptType -eq '{e3c9e316-0b5c-4db8-817d-f92df00215ae}' } 

	if ($vol -eq $null)	
	{	
		throw "Could not obtain the Reserved partition. Please, verify it is mounted and that your phone is in Mass Storage Mode"	
	}

	return $vol
}

function GetWindowsVolume()
{
	return GetVolume "WindowsARM" "NTFS"
}

function GetDataVolume()
{	
	return GetVolume "Data" "NTFS"
}

function GetSystemVolume()
{
	return GetVolume "BOOT" "FAT32"
}

function GetImageLetter()
{
	foreach ($disk in Get-Disk) 
	{
		foreach ($volume in Get-Volume) 	
		{
			if ($volume.DriveLetter -ne $null)
			{
				$path ="$($volume.DriveLetter):\sources\install.wim"
				$pathExist = Test-Path $path

				if ($pathExist -eq $true)
				{
					return $volume.DriveLetter
				}
			}
		}	
	}

	throw 'Could find INSTALL.WIM in any of the mounted drives. Please, verify that you mounted the .ISO drive correctly.'		 
}

function GetBootMgrPartitionPath()
{
    param([string] $bcdFileName)

    $bootMgrPartitionPath = bcdedit /store $bcdFileName /enum `{bootmgr`} |
      Select-String -Pattern '\{bootmgr\}' -context 1|
        ForEach-Object { ($_.Context.PostContext.Split('=')[1]) }

    if ($bootMgrPartitionPath -eq $null)
	{
        throw "Could not get the partition path of the {bootmgr} BCD entry"
    }

    return $bootMgrPartitionPath
}

function InstallDrivers()
{
	param([string] $driverFolder) 
	
	Write-Host "Working..."
	$windowsImageDrive = 'W:\'
	Dism /Image:$($windowsImageDrive) /Add-Driver /Driver:$($driverFolder) /Recurse /ForceUnsigned
}

function Get-BcdEntries() 
{
	param([string] $BcdPath)

	if ($BcdPath -ne "") 
	{
		$rawOutput = & bcdedit /store $BcdPath /enum all 
	} else {
		$rawOutput = & bcdedit /enum all 
	}

	$rawOutput = & bcdedit $BcdPath /enum all 
	$bcdOutput = $rawOutput -join "`n" -replace '\}\n\s+\{','},{'

	# Create the output list.
	$entries = New-Object System.Collections.Generic.List[pscustomobject]]

	# Parse bcdedit's output into entry blocks and construct a hashtable of
	# property-value pairs for each.
	($bcdOutput -split '(?m)^([a-z].+)\n-{10,100}\n').ForEach({
	  if ($_ -notmatch '  +') {
		$entries.Add([pscustomobject] @{ Name = $_; Properties = [ordered] @{} })
	  } else {
		($_ -split '\n' -ne '').ForEach({
		  $keyValue = $_ -split '\s+', 2
		  $entries[-1].Properties[$keyValue[0]] = $keyValue[1]
		})
	  }
	})
	
	return $entries 
}

function GetBcdEntryProperty()
{
	param([string] $entry, [string] $property)

	$regex = "$($property)\s*(?<value>[\w*\\]*)"
    $match = & bcdedit /enum $entry | Select-String $regex
    $bootMgrPartitionPath = $match.Matches[0].Groups[1].Value

    if ($bootMgrPartitionPath -eq $null)
    {
        throw "Could not get the given property $($property) from the entry $($entry)"
    }

    return $bootMgrPartitionPath
}

function Get-SystemInfo
{
  param($ComputerName = $env:COMPUTERNAME)
 
  $header = 'Hostname','OSName','OSVersion','OSManufacturer','OSConfiguration','OS Build Type','RegisteredOwner','RegisteredOrganization','Product ID','Original Install Date','System Boot Time','System Manufacturer','System Model','System Type','Processor(s)','BIOS Version','Windows Directory','System Directory','Boot Device','System Locale','Input Locale','Time Zone','Total Physical Memory','Available Physical Memory','Virtual Memory: Max Size','Virtual Memory: Available','Virtual Memory: In Use','Page File Location(s)','Domain','Logon Server','Hotfix(s)','Network Card(s)'
 
  $info = systeminfo.exe /FO CSV /S $ComputerName |
    Select-Object -Skip 1 | 
    ConvertFrom-CSV -Header $header
      
  $info.'System Locale'|%{$_.split(';')[0]}
}

function RemovePartition() 
{
	param([string] $label, [string] $fileSytemFormat)

	try 
	{
		$vol = GetVolume $label $fileSytemFormat
		$vol | Get-Partition | Remove-Partition -Confirm:$false
	}
	catch 
	{
		Write-Host "The $($label) partition couldn't be removed. It might not exist."
	}
}

function RemoveReserved() 
{
	try 
	{
		$part = GetReservedPartition
		$part | Remove-Partition -Confirm:$false
	} 
	catch 
	{
		Write-Host "The Reserved partition couldn't be removed. It might not exist."
	}	
}

function RemoveExistingWindowsPartitions() 
{
	RemoveReserved
	RemovePartition 'BOOT' 'FAT32'
	RemovePartition 'WindowsARM' 'NTFS'
}

function RemoveExistingWindowsPartitions() 
{
	RemoveReserved
	RemovePartition 'BOOT' 'FAT32'
	RemovePartition 'WindowsARM' 'NTFS'
}

function GetAvailableSpace()
{
	param($disk) 

	return $($disk.Size) -$($disk.AllocatedSize)		
}

function IsMoreSpaceNeeded() 
{
	Param ([switch]$verbose)
	$requiredGBs = 18
	$availableSpace = GetAvailableSpace $disk
	$spaceNeeded = $requiredGBs * 1000000000

	Write-Verbose "Available space: $($availableSpace)" 
	Write-Verbose "Required space: $($spaceNeeded)" 
	
	return $availableSpace -lt $spaceNeeded
}

function ShrinkDataPartition()
{
	$volume = GetDataVolume
	$driveLetter = $volume.DriveLetter

	Step "We are going to resize the Data partition at drive $($driveLetter) to 7.5GB. Please, verify the drive letter is correct."

	Write-Host "Working..."

	Resize-Partition -DriveLetter $driveLetter -Size 7.5GB

	Write-Host "Done!"
}


function GetPartitions()
{
	param($diskNumber)

	Get-Disk -Number $diskNumber | Get-Partition
}

function GetVolume()
{
	param($partitionId)

	Get-Partition -UniqueId $partitionId | Get-Volume
}

function ResizePartition()
{
	param($diskNumber, $partitionNumber, $sizeInBytes)

	Resize-Partition -DiskNumber $diskNumber -PartitionNumber $partitionNumber -Size $sizeInBytes
}