//===================

#include <stdio.h>
#include <tchar.h>
#include <conio.h>
#include <winsock2.h>
#include <math.h>
#include "NatNetTypes.h"
#include "NatNetServer.h"

// R*
#include "mmsystem.h"
#include "NATUtils.h"

#include <iterator>
#include <iostream>
#include <fstream>
#include <sstream>
#include <vector>
#include <string>
#include <functional>
//#include <ctime>
//#include <chrono>
//#include <thread>
#include "windows.h"
#include <time.h>

//#include <stdexcept>
// *R

#pragma warning( disable : 4996 )

#define STREAM_RBS 1
#define STREAM_MARKERS 1
#define STREAM_SKELETONS 1
#define STREAM_LABELED_MARKERS 1
#define FRAME_PLAY_THREAD_DELAY 500

#define csvParsePrint //myPrint 
#define printf myPrint 
#define voidfunction function<void(void)>
#define printFrameSend myPrint

using namespace std;

typedef struct
{
	string itemType;
	string itemName;
	string guid; // unclear what this is for
	string markerType;
	string variableName;


} sHeaderColumnItem;


void BuildDescription(sDataDescriptions* pDescription);
void SendDescription(sDataDescriptions* pDescription);
void FreeDescription(sDataDescriptions* pDescription);
void BuildFrame(long FrameNumber, sDataDescriptions* pDataDescriptions, sFrameOfMocapData* pOutFrame);
void SendFrame(sFrameOfMocapData* pFrame);
void FreeFrame(sFrameOfMocapData* pFrame);
void resetServer();
int CreateServer(int iConnectionType);
int HiResSleep(int msecs);

//R
vector<string> getNextLineCommaSeparatedTokens(istream &str, int skipCount);
void skipLines(istream &str, int skipCount); 
void exit(const char * format, ...);
void DefineFrameFileHeaderCols(string inputFilepath);
void ParseHeaderColumns(
	vector<sHeaderColumnItem> *pHeaderColumnItems,
	function<void(string)> onRigidBody,
	function<void(int, string)> onRigidBodyRotationVariable,
	function<void(int, string)> onRigidBodyPositionVariable,
	function<void(int)> onRigidBodyMeanMarkerError,
	function<void(string)> onRigidBodyMarker,
	function<void(int, string)> onRigidBodyMarkerPositionVariable,
	function<void(string)> onMarker,
	function<void(int)> onMarkerQuality,
	function<void(int, string)> onMarkerPositionVariable
	);
void PopulateDataDescriptionUsingFrameFileHeaderColumn(
	vector<sHeaderColumnItem>* headerColumnItems,
	sDataDescriptions* pDescription
	);
void SendFramesUsingFramesFile();
void myPrint(const char * format, ...);
void buildDataDescription();
char** arrayResize(char** oldArray, int currentSize, int newSize);
char** arrayPush(char **arr, int currentSize, const char *value);
ifstream getInputfileStream();
//R

// callbacks
void __cdecl MessageHandler(int msgType, char* msg);
int __cdecl RequestHandler(sPacket* pPacketIn, sPacket* pPacketOut, void* pUserData);

// playing thread
int StartPlayingThread();
int StopPlayingThread();
DWORD WINAPI PlayingThread_Func(void *dummy);
