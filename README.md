# CloudFS
The **CloudFS** library is a collection of .NET assemblies as gateways to various publicly accessible Cloud storage services.

[![License](https://img.shields.io/github/license/mashape/apistatus.svg)](https://github.com/viciousviper/CloudFS/blob/master/LICENSE.md)
[![Release](https://img.shields.io/github/tag/viciousviper/CloudFS.svg)](https://github.com/viciousviper/CloudFS/releases)
[![Version](https://img.shields.io/nuget/v/CloudFS.svg)](https://www.nuget.org/packages/CloudFS)
[![NuGet downloads](https://img.shields.io/nuget/dt/CloudFS.svg)](https://www.nuget.org/packages/CloudFS)
[![NuGet downloads (signed)](https://img.shields.io/nuget/dt/CloudFS-Signed.svg)](https://www.nuget.org/packages/CloudFS-Signed)

| Branch  | Build status | Code coverage | Code analysis | Quality gate |
| :------ | :----------: | :-----------: | :-----------: | :----------: |
| master  | [![Build status](https://ci.appveyor.com/api/projects/status/wjyq2wugi651ut0x/branch/master?svg=true)](https://ci.appveyor.com/project/viciousviper/cloudfs) | [![Coverage](https://codecov.io/github/viciousviper/CloudFS/coverage.svg?branch=master)](https://codecov.io/github/viciousviper/CloudFS?branch=master) | [![Code analysis](https://scan.coverity.com/projects/7864/badge.svg)](https://scan.coverity.com/projects/viciousviper-cloudfs) |[![Quality Gate](https://sonarqube.com/api/badges/gate?key=CloudFS)](https://sonarqube.com/dashboard/index/CloudFS) |
| develop | [![Build status](https://ci.appveyor.com/api/projects/status/wjyq2wugi651ut0x/branch/develop?svg=true)](https://ci.appveyor.com/project/viciousviper/cloudfs) | [![Coverage](https://codecov.io/github/viciousviper/CloudFS/coverage.svg?branch=develop)](https://codecov.io/github/viciousviper/CloudFS?branch=develop) | _see above_ | _see above_ |

## Objective

This library provides access to file system operations of various publicly accessible Cloud storage services behind a common interface. It thus facilitates the flexible integration of Cloud storage into arbitrary .NET applications.

## Supported Cloud storage services

Consideration of a cloud storage service as a target for CloudFS depends on these conditions:

- free storage space quota of at least 10 GB
- file expiration period no shorter than 90 days for free users
- *alternatively*, in response to a stated interest by the community, a cloud storage service may be included despite shortcomings in the preceding aspects
- availability of a .NET-accessible API under a non-invasive open source license (Apache, MIT, MS-PL)

Currently the following cloud storage services are supported in CloudFS via the specified API libraries:

| Cloud storage service                                            | API library                                                             | version    | sync/async | origin    | status | max. file size<sup id="a1">[1](#f1)</sup> |
| :--------------------------------------------------------------- | :---------------------------------------------------------------------- | :--------: | :--------: | :-------: | :----: | :------------: |
| *(local files)*                                                  | *System.IO (.NET Framework)*                                            | *N/A*      | *sync*     |           | stable | *N/A*          |
| [Google Drive](https://drive.google.com/ "Google Drive")         | [Google Apis V3](https://github.com/google/google-api-dotnet-client)    | 1.28.0.953 | async      | official  | stable | >= 256 MB      |
| [Box](https://app.box.com/ "Box")                                | [Box.V2](https://github.com/box/box-windows-sdk-v2)                     | 3.1.0      | async      | official  | stable | 128 MB         |
| [hubiC](https://hubic.com/ "hubiC")                              | [SwiftClient](https://github.com/vtfuture/SwiftClient)                  | 2.0.0-beta-0016 | async      | 3<sup>rd</sup> party | stable | 160 MB         |
| [MediaFire](https://www.mediafire.com "MediaFire")               | [MediaFire SDK](https://github.com/MediaFire/mediafire-csharp-open-sdk) | 1.0.0.3    | async      | 3<sup>rd</sup> party / local build | experimental | **12 MB**      |
| [MEGA](https://mega.co.nz/ "MEGA")                               | [MegaApiClient](https://github.com/gpailler/MegaApiClient)              | 1.6.0      | async      | 3<sup>rd</sup> party | stable | >= 256 MB      |
| [pCloud](https://www.pcloud.com/ "pCloud")                       | [pCloud.NET](https://github.com/nirinchev/pCloud.NET)                   | N/A        | async      | 3<sup>rd</sup> party / local build | stable | **16 MB**      |
| WebDAV<sup id="a5">[5](#f5)</sup>                                | [WebDAV Client](https://github.com/skazantsev/WebDavClient)             | 2.0.1      | async      | 3<sup>rd</sup> party | stable | >= 256 MB      |
| [Yandex Disk](https://disk.yandex.com/client/disk "Yandex Disk") | [Yandex Disk API Client](https://github.com/raidenyn/yandexdisk.client) | 1.2.11     | async      | 3<sup>rd</sup> party | stable | >= 256 MB      |
| **Degraded services**                                            |
| [Microsoft OneDrive](https://onedrive.live.com/ "OneDrive")<sup id="a2">[2](#f2)</sup> | [OneDrive SDK for CSharp](https://github.com/OneDrive/onedrive-sdk-csharp) | 2.0.7      | async      | official  | stable | >= 256 MB      |
| **Included by community request**                                |
| [Google Cloud Storage](https://cloud.google.com// "Google Cloud Storage") | [Google Cloud Libraries for .NET](https://github.com/GoogleCloudPlatform/google-cloud-dotnet) | 2.1.0-alpha02 | async      | official  | experimental | >= 256 MB      |
| **Superseded services**                                          |
| [Microsoft OneDrive](https://onedrive.live.com/ "OneDrive V1")   | [OneDrive SDK for CSharp](https://github.com/OneDrive/onedrive-sdk-csharp) | 1.2.0      | async      | official  | stable | **48 MB**      |
| [Microsoft OneDrive](https://onedrive.live.com/ "OneDrive-Legacy") | [OneDriveSDK](https://github.com/OneDrive/onedrive-explorer-win)<sup id="a3">[3](#f3)</sup> | N/A        | async      | inofficial  | obsolete | **48 MB**      |
| [Google Drive](https://drive.google.com/ "Google Drive V2")      | [Google Apis V2](https://github.com/google/google-api-dotnet-client)    | 1.28.0.953 | async      | official  | stable | >= 256 MB      |
| **Obsolete services**                                            |
| *[Copy](https://www.copy.com/ "Copy")*<sup id="a4">[4](#f4)</sup> | *[CopyRestAPI](https://github.com/saguiitay/CopyRestAPI)*              | *1.1.0*    | *async*    | *3<sup>rd</sup> party* | *retired* | *N/A*          |

> <sup><b id="f1">1</b></sup> Maximum supported file size for upload through the respective cloud API.<br/>This is a non-authoritative value determined through unit tests. [^](#a1)<br/>
> <sup><b id="f2">2</b></sup> Following Microsoft's November 2<sup>nd</sup>, 2015 announcement of its "[OneDrive storage plans change in pursuit of productivity and collaboration](https://blog.onedrive.com/onedrive_changes/)" the OneDrive cloud storage service will fail to meet the above stated requirements for support in CloudFS after mid-July 2016.<br/>Despite this unprecedented and highly objectionable degradation of service quality, OneDrive will continue to be supported by CloudFS for historical reasons. [^](#a2)<br/>
> <sup><b id="f3">3</b></sup> This version of OneDriveSDK has been deprecated by Microsoft. [^](#a3)<br/>
> <sup><b id="f4">4</b></sup> The Copy cloud storage service was discontinued as of May 1<sup>st</sup> 2016 according to this [announcement](https://www.copy.com/page/home;cs_login:login;;section:plans).<br/>The Copy gateway has therefore been retired from CloudFS. [^](#a4)<br/>
> <sup><b id="f5">5</b></sup> WebDAV-based cloud storage is available through various public cloud providers or by self-hosting an [OwnCloud](https://github.com/owncloud) private cloud. [^](#a5)<br/>


## System Requirements

- Platform
  - .NET 4.6.2
- Operating system
  - tested on Windows 8.1 x64 and Windows Server 2012 R2 (until version 1.0.0-alpha) /<br/>Windows 10 x64 (from version 1.0.1-alpha)
  - expected to run on Windows 7/8/8.1/10 and Windows Server 2008(R2)/2012(R2)/2016

## Local compilation

Several cloud storage services require additional authentication of external applications for access to cloud filesystem contents.<br/>For cloud storage services with this kind of authentication policy in place you need to take the following steps before compiling CloudFS locally:

- register for a developer account with the respective cloud service
- create a cloud application configuration with sufficient rights to access the cloud filesystem
- enter the service-provided authentication details into the prepared fields in the `Secrets` class of the affected PowerShellCloudProvider gateway project

At the time of writing this Readme, the following URLs provided access to application management tasks such as registering a new application or changing an application's configuration:

| Cloud storage service | Application registration / configuration URL           |
| :-------------------- | :----------------------------------------------------: |
| Microsoft OneDrive    | [Microsoft Account - Developer Center](https://account.live.com/developers/applications/index) |
| Google Drive          | [Google Developers Console](https://console.developers.google.com) |
| Box                   | [Box Developers Services](https://app.box.com/developers/services/edit/) |
| hubiC                 | [Develop hubiC applications](https://hubic.com/home/browser/developers/) |
| MediaFire             | [MediaFire - Developers](https://www.mediafire.com/index.php#settings/applications) |
| MEGA                  | [Mega Core SDK - Developers](https://mega.nz/#sdk)     |
| pCloud                | *- no configuration required -*                        |
| WebDAV                | *- no configuration required -*
| Yandex Disk           | [Yandex OAuth Access](https://oauth.yandex.com/)       |
| Google Cloud Storage  | [Google Cloud Platform Console](https://console.cloud.google.com/) |
| **Obsolete**          |                                                        |
| <del>Copy</del>       | <del>[Copy Developers - Applications]()</del>          |

## Release Notes

| Date       | Version     | Comments                                                                       |
| :--------- | :---------- | :----------------------------------------------------------------------------- |
| 2017-03-31 | 1.0.11-beta | - Version updates to API libraries for various cloud services.<br/>- Moved legacy gateways to separate NuGet package.<br/>- Switched to [Polly](https://github.com/App-vNext/Polly) for error retry functionality in gateways. |
| 2016-10-05 | 1.0.10.1-beta | - Fixed NuGet packages.                                                     |
| 2016-10-01 | 1.0.10-beta | - New gateway for Google Cloud Storage added.<br/>- Fixed drive free space calculation for Yandex gateway.<br/>- Version update to API libraries for Box, Google Drive, and Yandex.Disk. |
| 2016-08-31 | 1.0.9.1-alpha | - Fixed NuGet packages.                                                      |
| 2016-08-29 | 1.0.9-alpha | - Implemented settings purge function in gateways.<br/>- Version updates to API libraries for Box and OneDrive. |
| 2016-08-26 | 1.0.8-alpha | - New gateway for generic WebDAV providers added.<br/>- Support AES encryption of account credentials and access tokens in locally persisted application settings.<br/>- Fixed concurrent access to locally persisted application settings.<br/>- Version updates to API libraries for Box, Google Drive, SwiftClient, and SemanticTypes.<br/>- Activated static code analysis via Coverity Scan. |
| 2016-08-07 | 1.0.7-alpha | - Added an explicit authentication method for cloud gateways. All other gateway methods still require successful authentication to the cloud service. **Note:** This breaks compatibility with the previous versions of `ICloudGateway` and `IAsyncCloudGateway`.<br/>- Migrated gateway for Google Drive and OneDrive to Google Drive API v3 and OneDriveSDK, respectively (previously used API libraries remain available via *_Legacy gateways).<br/>- MediaFire gateway now supports session token v2 (lifetime 2 years instead of 10 minutes)<br/>- OneDrive gateway now supports creation of empty files.<br/>- Fixed cross-thread marshalling of authentication tokens.<br/>- Version updates to API libraries for Box, Google Drive, OneDrive |
| 2016-05-20 | 1.0.6-alpha | - Fixed broken package references in NuGet specs (present since 1.0.3-alpha)<br/>- Version update to API library for Box |
| 2016-05-18 | 1.0.5-alpha | - Retired gateway for Cloud<br/>- Version update to API library for Google Drive<br/>- Support for Windows Explorer new file creation sequence in MEGA<br/>- Improved online editing capability in non-encrypting File gateway
| 2016-04-17 | 1.0.4-alpha | - New gateway for hubiC/Swift added.<br/>- Version updates to API libraries for Google Drive, MEGA, and Yandex Disk.<br/>- Converted Mega gateway to Async operation mode.<br/>- Gateways now explicitely declare their capabilities in the ExportMetadata.<br/>- Improvements to login window handling if logins are requested for multiple drives.<br/>- Various bug fixes. |
| 2016-02-01 | 1.0.3-alpha | - New gateways for MediaFire and Yandex Disk added.                            |
| 2016-01-24 | 1.0.2-alpha | - Gateway configuration extended to accept custom parameters. This change **breaks compatibility** with earlier API versions.<br/>- File Gateway now configurable with target root directory |
| 2016-01-19 | 1.0.1-alpha | - NuGet dependencies updated, schema of App.config in tests project refactored |
| 2016-01-08 | 1.0.0-alpha | - Initial release and NuGet registration                                       |
| 2015-12-29 | 1.0.0.0     | - Initial commit                                                               |

## Future plans

- include additional gateways for more Cloud storage services
- improve stability of large file uploads# sslproject1
# gittest
# gittest pavan
