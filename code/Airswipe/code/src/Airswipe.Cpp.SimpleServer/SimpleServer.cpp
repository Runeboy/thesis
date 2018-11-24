//=============================================================================
// Copyright © 2010 NaturalPoint, Inc. All Rights Reserved.
// 
// This software is provided by the copyright holders and contributors "as is" and
// any express or implied warranties, including, but not limited to, the implied
// warranties of merchantability and fitness for a particular purpose are disclaimed.
// In no event shall NaturalPoint, Inc. or contributors be liable for any direct,
// indirect, incidental, special, exemplary, or consequential damages
// (including, but not limited to, procurement of substitute goods or services;
// loss of use, data, or profits; or business interruption) however caused
// and on any theory of liability, whether in contract, strict liability,
// or tort (including negligence or otherwise) arising in any way out of
// the use of this software, even if advised of the possibility of such damage.
//=============================================================================


/*

SimpleServer.cpp

Illustrates the minimum code required to create and send marker data using
the NatNet server.

*/

#include "SimpleServer.h"


// *** VARIABLES ***

vector<sHeaderColumnItem> headerColumnItems; //R


NatNetServer* theServer;    					// The NatNet Server 
sDataDescriptions descriptions; 				// Describes what is sent (Marker sets, rigid bodies, etc)
long g_lCurrentFrame = 0;
bool g_bPlaying = false;
DWORD PlayingThread_ID = NULL;
HANDLE PlayingThread_Handle = NULL;
int counter = 0;
int counter2 = 0;
float fCounter = 0.0f;

unsigned int MyDataPort = 3130;
unsigned int MyCommandPort = 3131;
unsigned long lAddresses[10];

// Single frame of data (for all tracked objects)
sFrameOfMocapData frameToSend;

bool isContinuousSendEnabled = false;
bool isPrintEnabled = true;
bool useInputFile = false;
string inputFilepath; 

// *** METHODS ***

int _tmain(int argc, _TCHAR* argv[])
{
	// Create a NatNet server
	int iConnectionType = ConnectionType_Multicast;
	//int iConnectionType = ConnectionType_Unicast;
	int iResult = CreateServer(iConnectionType);
	if (iResult != ErrorCode_OK)
	{
		printf("Error initializing server.  See log for details.  Exiting");
		return 1;
	}

	if (argc > 1){
		inputFilepath = argv[1]; //"c:\\csv.csv";
		//inputFilepath = "c:\\csv.csv";
		printf("Input file given as parameter: %s\n", inputFilepath.c_str());
		useInputFile = true;
	}
	
	buildDataDescription();

	// OK! Ready to stream data.  Listen for request from clients (RequestHandler())
	printf("\n\nCommands:\nn\tnext frame\ns\tstream frames\nr\treset server\nq\tquit\nm\tmulticast\nu\tunicast \nc\ttoggle continuous frame send \nf\ttoggle data source dummy/file\n\n");
	bool bExit = false;
	while (int c = _getch())
	{		
		switch (c) {
			case 'n':
			{							// next frame
				sFrameOfMocapData frame;
				BuildFrame(g_lCurrentFrame++, &descriptions, &frame);
				SendFrame(&frame);
				FreeFrame(&frame);
			}
			break;
			case 'q':                           // quit
				bExit = true;
				break;
			case 's': {							// play continuously
					if (useInputFile)
						SendFramesUsingFramesFile();
					else {
						g_lCurrentFrame = 0;
						if (g_bPlaying)
							StopPlayingThread();
						else
							StartPlayingThread();
					}
				}
				break;
			case 'r':	                        // reset server
				resetServer();
				break;
			case 'm':	                        // change to multicast
				iResult = CreateServer(ConnectionType_Multicast);
				if (iResult == ErrorCode_OK)
					printf("Server connection type changed to Multicast.\n\n");
				else
					printf("Error changing server connection type to Multicast.\n\n");
				break;
			case 'u':	                        // change to unicast
				iResult = CreateServer(ConnectionType_Unicast);
				if (iResult == ErrorCode_OK)
					printf("Server connection type changed to Unicast.\n\n");
				else
					printf("Error changing server connection type to Unicast.\n\n");
				break;
			case 'f': {
					useInputFile = !useInputFile;
					printf("Frame data description and broadcast send now based on input file: %s\n", useInputFile? "YES":"NO");
					buildDataDescription();
				}
				break;				
			case 'c': {
					isContinuousSendEnabled = !isContinuousSendEnabled;
					printf("Is continuous frame send enabled: %s\n", isContinuousSendEnabled? "YES" : "NO");
				}
				break;
			default:
				break;
		}

		if (bExit)
		{
			theServer->Uninitialize();
			FreeDescription(&descriptions);
			break;
		}

		printf("\nAwaiting command..\n");
	}

	return ErrorCode_OK;
}

void buildDataDescription() {
	if (useInputFile)  {
		printf("Input frame file specified: %s\n", inputFilepath.c_str());
		DefineFrameFileHeaderCols(inputFilepath);
		PopulateDataDescriptionUsingFrameFileHeaderColumn(&headerColumnItems, &descriptions);
	}
	else
		BuildDescription(&descriptions);
}

void myPrint(const char * format, ...) {
	if (!isPrintEnabled)
		return;

	va_list argptr;
	va_start(argptr, format);

	vfprintf(stderr, format, argptr);
	va_end(argptr);
}

void skipLines(istream &str, int skipCount) {
	printf("Skipping %i line(s)\n ", skipCount);
	
	string line;
	while (skipCount-- > 0 && getline(str, line)) { }
}

vector<string> getNextLineCommaSeparatedTokens(istream &str, int skipCount) {
	vector<string>   result;
	string                line;
	getline(str, line);

	stringstream          lineStream(line);
	string                cell;

	while (getline(lineStream, cell, ','))
		if ((skipCount--) <= 0)
			result.push_back(cell);

	return result;
}

void exit(const char * format, ...) {
	va_list argptr;
	va_start(argptr, format);
	vfprintf(stderr, format, argptr);
	va_end(argptr);

	printf("\n\n");

	exit(1);
}

//bool isHeaderLine(int lineNumber) { return lineNumber <= 5; }

//sRigidBodyData consumeRidigBody(int i) {
//
//
//	sRigidBodyData s;
//
//	return s;
//}

//void ensureFileExists(string filepath) {
//
//}

void DefineFrameFileHeaderCols(string inputFilepath) {
	printf("Defining frame file header columns from input file..\n");

	ifstream file = getInputfileStream();
	skipLines(file, 2); // first two lines are irrelevant info

	descriptions.nDataDescriptions = 0;

	vector<sRigidBodyData> ridigBodies;
	vector<string> rowHeaderTokens;// = getNextLineCommaSeparatedTokens(file);
	vector<string> itemTypeTokens;
	vector<string> itemNameTokens;
	vector<string> itemGuidTokens;
	vector<string> markerTypeTokens;
	vector<string> variableNameTokens;


	headerColumnItems.clear();

	int lineNumber = 0;
	while (
		++lineNumber <= 5 // always true, we'll need the line number in the next line
		) {
		rowHeaderTokens = getNextLineCommaSeparatedTokens(file, 2); // when processing header lines, the first two columns are empty (so we skip'em)

		//printf("?? %i\n", lineNumber);
		//printf("\t\t %i\n", rowHeaderTokens.size());

		if (lineNumber == 1)
			itemTypeTokens = rowHeaderTokens;
		else if (lineNumber == 2)
			itemNameTokens = rowHeaderTokens;
		else if (lineNumber == 3)
			itemGuidTokens = rowHeaderTokens;
		else if (lineNumber == 4)
			markerTypeTokens = rowHeaderTokens;
		else if (lineNumber == 5)
			variableNameTokens = rowHeaderTokens;
	}
	file.close();

	int headerTokenCount = rowHeaderTokens.size();

	//printf("-- %i\n", itemGuidTokens.size());
	//printf("-- %i\n", itemNameTokens.size());
	//printf("-- %i\n", itemTypeTokens.size());
	//printf("-- %i\n", markerTypeTokens.size());
	//printf("-- %i\n", variableNameTokens.size());


	for (int tokenIndex = 0; tokenIndex < headerTokenCount; tokenIndex++) {
		sHeaderColumnItem colItem;
		colItem.guid = itemGuidTokens[tokenIndex];
		colItem.itemName = itemNameTokens[tokenIndex];
		colItem.itemType = itemTypeTokens[tokenIndex];
		colItem.markerType = markerTypeTokens[tokenIndex];
		colItem.variableName = variableNameTokens[tokenIndex];

		headerColumnItems.push_back(colItem);
	}

	printf("Frame file header token count: %i\n", headerTokenCount);

}

//void RuneMakeDesc(sDataDescriptions *pDataDescription, function<void(double)> f) {
//	
//	vector<sHeaderColumnItem> cols = headerColumnItems;
//	for (int i = 0; i < headerColumnItems.size(); i++) { // parse each column sequentially from left to right, as is the convention used in the frame csv file 
//		sHeaderColumnItem col = headerColumnItems[i];
//		bool isFirstColumn = (i == 0);
//		//auto func = [](int i) -> double { return 2 * i / 1.15; };
//		//double d = func(1);
//
//		//RigidBodyParsed fp = &func;
//		//int a = fp(3.14);
//
//		printf("\tProcessing column %i/%i\n", i + 1, headerColumnItems.size());
//		bool isNewItemGuid = (isFirstColumn || col.guid != cols[i - 1].guid);
//		bool isNewItemName = (isFirstColumn || col.itemName != cols[i - 1].itemName);
//
//		
////#if STREAM_RBS
////			// Rigid Body Description
////			for (int i = 0; i < 20; i++)
////			{
////				sRigidBodyDescription* pRigidBodyDescription = new sRigidBodyDescription();
////				sprintf(pRigidBodyDescription->szName, "RigidBody %d", i);
////				pRigidBodyDescription->ID = i;
////				pRigidBodyDescription->offsetx = 1.0f;
////				pRigidBodyDescription->offsety = 2.0f;
////				pRigidBodyDescription->offsetz = 3.0f;
////				pRigidBodyDescription->parentID = 2;
////				pDescription->arrDataDescriptions[index].type = Descriptor_RigidBody;
////				pDescription->arrDataDescriptions[index].Data.RigidBodyDescription = pRigidBodyDescription;
////				pDescription->nDataDescriptions++;
////				index++;
////			}
////#endif
////
//
//
//	}
//}

//typedef void(*RigidBodyParsed) (sRigidBodyData r);
//int f(double d) { return 999; }


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
	) {
	vector<sHeaderColumnItem> cols = *pHeaderColumnItems;

	//vector<sHeaderColumnItem> cols = headerColumnItems;
	for (int i = 0; i < headerColumnItems.size(); i++) { // parse each column sequentially from left to right, as is the convention used in the frame csv file 
		csvParsePrint("\tProcessing column %i/%i\n", i + 1, headerColumnItems.size());

		bool isFirstColumn = (i == 0);

		bool isNewItemGuid = (isFirstColumn || cols[i].guid != cols[i - 1].guid);
		bool isNewItemName = (isFirstColumn || cols[i].itemName != cols[i - 1].itemName);

		string itemType = cols[i].itemType;
		string markerType = cols[i].markerType;
		string variableName = cols[i].variableName;
		//float value = values[i];

		if (itemType == "Rigid Body") {
			string rigidBodyName = cols[i].itemName;
			if (isNewItemGuid) {
				if (onRigidBody != NULL)
					onRigidBody(rigidBodyName);
			}

			if (markerType == "Rotation") {
				if (onRigidBodyRotationVariable != NULL)
					onRigidBodyRotationVariable(i, variableName);
			}
			else if (markerType == "Position") {
				if (onRigidBodyPositionVariable != NULL)
					onRigidBodyPositionVariable(i, variableName);

			}
			else if (markerType == "Error Per Marker") {
				if (onRigidBodyMeanMarkerError != NULL)
					onRigidBodyMeanMarkerError(i);
			}
			else exit("Could not parse RidigBody item type token '%s'", markerType.c_str());
		}
		else if (itemType == "Rigid Body Marker") {
			if (isNewItemName) {
				string markerName = cols[i].itemName;
				if (onRigidBodyMarker != NULL)
					onRigidBodyMarker(markerName);
			}

			if (markerType == "Position") {
				if (onRigidBodyMarkerPositionVariable != NULL)
					onRigidBodyMarkerPositionVariable(i, variableName);
			}
			else if (markerType == "Marker Quality") {
				//pRB-> MeanError = value; 
				// TODO: not used?
				if (onMarkerQuality != NULL)
					onMarkerQuality(i);
			}
			else exit("Could not parse RidigBody Marker item type token '%s'", markerType.c_str());

		}
		else if (itemType == "Marker") {
			string markerName = cols[i].itemName;
			if ((isNewItemName) && onMarker != NULL)
				onMarker(markerName);

			if (markerType == "Position") {
				if (onMarkerPositionVariable != NULL)
					onMarkerPositionVariable(i, variableName);
			}
			else exit("Could not parse Marker marker item type token '%s'", markerType.c_str());
		}
		else {
			exit("\t\tCould not parse item type '%s'", itemType.c_str());
		}
	}
}

char** arrayResize(char** oldArray, int currentSize, int newSize) {
	char** newArray = new char*[newSize]; // will now contain an empty slot at last index
	if (oldArray != NULL)
		for (int i = 0; i < currentSize; i++) 
			if (i < newSize) {
				newArray[i] = new char[MAX_NAMELENGTH];
				strcpy(newArray[i], oldArray[i]); //szTemp);
			}

	delete oldArray; // OK?

	return newArray;
}

char** arrayPush(char **arr, int currentSize, const char *value) {
	char** newArray = arrayResize(arr, currentSize, currentSize + 1);
	newArray[currentSize] = new char[MAX_NAMELENGTH];
	strcpy(newArray[currentSize], value);
	
	return newArray;
}

void PopulateDataDescriptionUsingFrameFileHeaderColumn(
	vector<sHeaderColumnItem>* headerColumnItems,
	sDataDescriptions* pDescription
	) {
	sDataDescription *lastDataDescription;

	vector<string> ridigBodyNames;
	vector<string> markerNames;

	auto onRigidBody = [&lastDataDescription, pDescription, &ridigBodyNames](string rigidBodyName) -> void {
		csvParsePrint("\t\tParsing new item: Rigid Body\n");
		ridigBodyNames.push_back(rigidBodyName);

		sRigidBodyDescription* pRigidBodyDescription = new sRigidBodyDescription();

		int index = pDescription->nDataDescriptions;
		
		pDescription->arrDataDescriptions[index].type = Descriptor_RigidBody;
		pDescription->arrDataDescriptions[index].Data.RigidBodyDescription = pRigidBodyDescription;
		pDescription->nDataDescriptions++;
		
		pRigidBodyDescription->ID = index + 1;
		sprintf(pRigidBodyDescription->szName, rigidBodyName.c_str());

		lastDataDescription = &pDescription->arrDataDescriptions[index];
		lastDataDescription->Data.MarkerSetDescription->nMarkers = 0; // this defaults to 1 on initializing the rigidbody-description, should intuitively be 0 

		csvParsePrint("\t\tAdded rigidbody '%s' as description number %i to data description\n", rigidBodyName.c_str(), index + 1);
	};

	auto onRigidBodyMarker = [&lastDataDescription, &pDescription, &markerNames](string markerName) -> void {
		csvParsePrint("\t\tParsing new item: RigidBody Marker\n");
		markerNames.push_back(markerName);	

		if (lastDataDescription->type != Descriptor_RigidBody)
			exit("Last data description was not a rigid body which breaks the expected format, exiting..");

		sMarkerSetDescription* pMarkerSetDescription = lastDataDescription->Data.MarkerSetDescription;
		
		pMarkerSetDescription->szMarkerNames = arrayPush(
			pMarkerSetDescription->szMarkerNames,
			pMarkerSetDescription->nMarkers,
			markerName.c_str()
			);
		pMarkerSetDescription->nMarkers++;

		csvParsePrint("\t\tAdded rigidbody marker '%s' as marker number %i of marker set of last parsed data description\n", markerName.c_str(), pMarkerSetDescription->nMarkers);
	};

	auto onMarker = [&pDescription, &lastDataDescription, &markerNames](string markerName) -> void {
		csvParsePrint("\t\tParsing new item: Named Marker\n");
		markerNames.push_back(markerName);

		sMarkerSetDescription* pMarkerSetDescription;
		if (lastDataDescription->type == Descriptor_MarkerSet)
			pMarkerSetDescription = lastDataDescription->Data.MarkerSetDescription;
		else  {
			pMarkerSetDescription = new sMarkerSetDescription();
			pMarkerSetDescription->nMarkers = 0;

			int index = pDescription->nDataDescriptions;
			pDescription->arrDataDescriptions[index].type = Descriptor_MarkerSet;
			pDescription->arrDataDescriptions[index].Data.MarkerSetDescription = pMarkerSetDescription;
			pDescription->nDataDescriptions++;
			//index++;

			lastDataDescription = &pDescription->arrDataDescriptions[index];

			csvParsePrint("\t\tAdded new markerset as data description number %i\n", index + 1);
		}


		pMarkerSetDescription->szMarkerNames = arrayPush(
			pMarkerSetDescription->szMarkerNames,
			pMarkerSetDescription->nMarkers,
			markerName.c_str()
			);
		pMarkerSetDescription->nMarkers++;

		csvParsePrint("\t\tAdded single marker '%s' as marker number %i to marker set of last data description\n", markerName.c_str(), pMarkerSetDescription->nMarkers);
	};

	//auto onMarkerQuality = [](int colIndex) -> void {
	//	//printf("\t\tParsed RigidBody Marker Quality value=%f\n", value);
	//};

	//auto onMarkerPositionVariable = [&lastMarker](int colIndex, string variableName) -> void {
	//	//printf("\t\tParsed Marker Position variable '%s'=%f\n", variableName.c_str(), value);
	//};

//	if (1==2)
	ParseHeaderColumns(
		headerColumnItems,
		onRigidBody,
		NULL, //onRigidBodyRotationVariable,
		NULL, //onRigidBodyPositionVariable,
		NULL, //onRigidBodyMeanMarkerError,
		onRigidBodyMarker,
		NULL, //onRigidBodyMarkerPositionVariable,
		onMarker,
		NULL,//onMarkerQuality,
		NULL //onMarkerPositionVariable
		);

	printf("Parse result:\n");
	printf("\tRidig bodies: ");
	for each(string name in ridigBodyNames)
		printf("'%s', ", name.c_str());
	printf("\n\tMarkers parsed: ");
	for each(string name in markerNames)
		printf("'%s', ", name.c_str());
	printf("\n");
}



void PopulateFrameUsingFrameFileColumnValues(
	sFrameOfMocapData *pOutFrame, 
	vector<float> values,
	vector<sHeaderColumnItem> *pHeaderColumnItems
	) {	
	sRigidBodyData* lastRigidBody = NULL;
	MarkerData* lastRigidBodyMarker = NULL;
	sMarker* lastMarker = NULL;
	
	//vector<sHeaderColumnItem> cols = *headerColumnItems;

	auto onRigidBody = [&lastRigidBody, pOutFrame](string rigidBodyName) -> void {
		csvParsePrint("\t\tParsing new item: Rigid Body\n");

		lastRigidBody = &pOutFrame->RigidBodies[pOutFrame->nRigidBodies];
		pOutFrame->nRigidBodies++;
		lastRigidBody->ID = (pOutFrame->nRigidBodies);
		lastRigidBody->Markers = new MarkerData[100]; //TODO:  100 slots harcoded -make dynamic?? 
	};

	auto onRigidBodyRotationVariable = [&lastRigidBody, values](int colIndex, string variableName) -> void {
		float value = values[colIndex];
		csvParsePrint("\t\tParsed Rotation variable '%s'=%f\n", variableName.c_str(), value);

		if (variableName == "X")
			lastRigidBody->qx = value;
		else if (variableName == "Y")
			lastRigidBody->qy = value;
		else if (variableName == "Z")
			lastRigidBody->qz = value;
		else if (variableName == "W")
			lastRigidBody->qw = value;
		else exit("Could not parse variable name '%s' for RigidBody Rotation", variableName.c_str());
	};

	auto onRigidBodyPositionVariable = [&lastRigidBody, values](int colIndex, string variableName) -> void {
		float value = values[colIndex];
		csvParsePrint("\t\tParsed Position variable '%s'=%f\n", variableName.c_str(), value);

		if (variableName == "X")
			lastRigidBody->x = value;
		else if (variableName == "Y")
			lastRigidBody->y = value;
		else if (variableName == "Z")
			lastRigidBody->z = value;
		else exit("Could not parse variable name '%s' for RigidBody Position", variableName.c_str());
	};

	auto onRigidBodyMeanMarkerError = [&lastRigidBody,values](int colIndex) -> void {
		float value = values[colIndex];
		csvParsePrint("\t\tParsed RigidBody Error-Per-Marker value=%f\n", value);

		lastRigidBody->MeanError = value;
	};

	auto onRigidBodyMarker = [&lastRigidBodyMarker, &lastRigidBody](string markerName) -> void {
		csvParsePrint("\t\tParsing new item: RigidBody Marker\n");

		lastRigidBodyMarker = &lastRigidBody->Markers[lastRigidBody->nMarkers];
		lastRigidBody->nMarkers++;
	};

	auto onRigidBodyMarkerPositionVariable = [&lastRigidBodyMarker, values](int colIndex, string variableName) -> void {
		float value = values[colIndex];
		csvParsePrint("\t\tParsed RidigBody Marker Position variable '%s'=%f\n", variableName.c_str(), value);

		if (variableName == "X") //               // posX, posY, posZ
			(*lastRigidBodyMarker)[0] = value;
		else if (variableName == "Y")
			(*lastRigidBodyMarker)[1] = value;
		else if (variableName == "Z")
			(*lastRigidBodyMarker)[2] = value;
		else exit("Could not parse variable name '%s' for RigidBody Marker Position", variableName.c_str());
	};
		
	auto onMarker = [&lastMarker, &pOutFrame](string markerName) -> void {
		csvParsePrint("\t\tParsing new item: Named Marker\n");

		lastMarker = &pOutFrame->LabeledMarkers[pOutFrame->nLabeledMarkers];
		pOutFrame->nLabeledMarkers++;
		lastMarker->ID = pOutFrame->nLabeledMarkers;
	};

	auto onMarkerQuality = [values](int colIndex) -> void {
		float value = values[colIndex];
		csvParsePrint("\t\tParsed RigidBody Marker Quality value=%f\n", value);
	};

	auto onMarkerPositionVariable = [&lastMarker, values](int colIndex, string variableName) -> void {
		float value = values[colIndex];
		csvParsePrint("\t\tParsed Marker Position variable '%s'=%f\n", variableName.c_str(), value);

		if (variableName == "X") //               // posX, posY, posZ
			lastMarker->x = value;
		else if (variableName == "Y")
			lastMarker->y = value;
		else if (variableName == "Z")
			lastMarker->z = value;
		else exit("Could not parse variable name '%s' for Marker Position", variableName.c_str());
	};

	ParseHeaderColumns(
		pHeaderColumnItems,
		onRigidBody,
		onRigidBodyRotationVariable,
		onRigidBodyPositionVariable,
		onRigidBodyMeanMarkerError,
		onRigidBodyMarker,
		onRigidBodyMarkerPositionVariable,
		onMarker,
		onMarkerQuality,
		onMarkerPositionVariable
		);
}

ifstream getInputfileStream() {
	ifstream file(inputFilepath);

	if (!file.good()) {
		exit("file '%s' does not exist, exiting..\n\n", inputFilepath.c_str());
	}

	return file;
}

void SendFramesUsingFramesFile() {
	printf("Sending frames using input file..\n");

	if (headerColumnItems.size() == 0) {
		printf("\nCan't read values yet, as header colums items have not been defined.\n");
		return;
	}

	int repetitionCount = 0;
	int totalFrameCount = 0;
	while (repetitionCount == 0 || isContinuousSendEnabled)
	{
		int delayedFrameCount = 0;
		int totalDelay = 0;
		int frameCount = 0;


		ifstream file = getInputfileStream();
		printf("\nProcessing file values from the beginning..\n");
		skipLines(file, 7);

		vector<string> rowTokens;

		unsigned long startTime = GetTickCount();

		int lineNumber = 0;
		while (
			&(rowTokens = getNextLineCommaSeparatedTokens(file, 0)) && // affects the next while-condition
			file // will be false if previous assignment results in no more lines to process
			) {
			lineNumber++;

			//		sFrameOfMocapData frame;
			FreeFrame(&frameToSend);
			ZeroMemory(&frameToSend, sizeof(sFrameOfMocapData));
			frameToSend.iFrame = totalFrameCount; // always use strictly increasing frame IDs 
			//frameToSend.MocapDa

			int frameIndex = stoi(rowTokens[0].c_str());
			unsigned long timeDeltaMilliseconds = atof(rowTokens[1].c_str()) * 1000;

			//clock_t begin_time = clock();
			vector<string> rowHeaderTokens(rowTokens.begin() + 2, rowTokens.end());

			vector<float> values;
			for each (string token in rowHeaderTokens)
				values.push_back(atof(token.c_str()));
			//printf("done\n");

			if (values.size() != headerColumnItems.size()) {
				printf("header columns vs. values columns size mismatch (%i != %i) \n", values.size(), headerColumnItems.size());
				break;
			}

			//std::this_thread::sleep_for(std::chrono::seconds(time));

			PopulateFrameUsingFrameFileColumnValues(&frameToSend, values, &headerColumnItems);
			SendFrame(&frameToSend);

//			printf(".");

			frameCount++;
			totalFrameCount++;

			unsigned long milliSecondsPassed = GetTickCount() - startTime;
			int millisecondsToSleep = timeDeltaMilliseconds - milliSecondsPassed;

			if (millisecondsToSleep < 0) {
				delayedFrameCount++;
				totalDelay += millisecondsToSleep * -1;

				printf("-"); // delayed frame indicator
			}
			else
				Sleep(millisecondsToSleep);
		}
		
		file.close();
		repetitionCount++;

		unsigned long milliSecondsPassed = GetTickCount() - startTime;

		printf("\n\ndone reading csv\n");
		printf("\tframe count: %i\n", frameCount);
		printf("\tdelayed frame count: %i\n", delayedFrameCount);
		printf("\tpercent delayed frames: %f\n", 100.0 * delayedFrameCount / frameCount);
		printf("\tavg delay: %f  ms\n", (double)totalDelay / delayedFrameCount);
		printf("\tframerate: %f fps\n", (double)frameCount/milliSecondsPassed*1000);

	}

	//cout << ((double)totalDelay / delayedFrameCount);
	
}


int CreateServer(int iConnectionType)
{
	// release previous server
	if (theServer)
	{
		theServer->Uninitialize();
		delete theServer;
	}

	// create new NatNet server
	theServer = new NatNetServer(iConnectionType);

	// optional - use old multicast group
	//theServer->SetMulticastAddress("224.0.0.1");

	// print version info
	unsigned char ver[4];
	theServer->NatNetVersion(ver);
	printf("NatNet Simple Server (NatNet ver. %d.%d.%d.%d)\n", ver[0], ver[1], ver[2], ver[3]);

	// set callbacks
	theServer->SetErrorMessageCallback(MessageHandler);
	theServer->SetVerbosityLevel(Verbosity_Debug);
	theServer->SetMessageResponseCallback(RequestHandler);	    // Handles requests from the Client

	// get local ip addresses
	char szIPAddress[128];
	in_addr MyAddress[10];
	int nAddresses = NATUtils::GetLocalIPAddresses2((unsigned long *)&MyAddress, 10);
	if (nAddresses < 1)
	{
		printf("Unable to detect local network.  Exiting");
		return 1;
	}

	//int badPrefixes[2] = { 10, 168 };
	printf("\n *** SELECTING FIRST ADDRESS THAT STARTS WITH '10' OR '168' OR '192'! ***\n");

	const int NONE_SELECTED = -1;
	int selectedIpAddress = NONE_SELECTED;
	for (int i = 0; i < nAddresses; i++)
	{
		sprintf(szIPAddress, "%d.%d.%d.%d", MyAddress[i].S_un.S_un_b.s_b1, MyAddress[i].S_un.S_un_b.s_b2, MyAddress[i].S_un.S_un_b.s_b3, MyAddress[i].S_un.S_un_b.s_b4);
		printf("IP Address : %s", szIPAddress);

		UCHAR prefix = MyAddress[i].S_un.S_un_b.s_b1;
		if (selectedIpAddress == NONE_SELECTED && (prefix == 10 || prefix == 168 || prefix == 192)) {
			selectedIpAddress = i;
			printf("\t(SELECTED)");
		}

		printf("\n");
	}
	// select first

	sprintf(
		szIPAddress, "%d.%d.%d.%d",
		MyAddress[selectedIpAddress].S_un.S_un_b.s_b1,
		MyAddress[selectedIpAddress].S_un.S_un_b.s_b2,
		MyAddress[selectedIpAddress].S_un.S_un_b.s_b3,
		MyAddress[selectedIpAddress].S_un.S_un_b.s_b4
		);

	printf("\n");

	// Initialize NatNet server with first detected ip address - use NatNet default port assignments
	int retCode = theServer->Initialize(szIPAddress);
	// to use a different port for commands and/or data:
	//int retCode = theServer->Initialize(szIPAddress, MyCommandPort, MyDataPort);
	if (retCode != 0)
	{
		printf("Error initializing server.  See log for details.  Exiting");
		return 1;
	}
	else
	{
		printf("Server initialized on %s.\n", szIPAddress);
	}

	// print address/port info
	char szCommandIP[24];
	char szDataIP[24];
	char szMulticastGroup[24];
	int iDataPort, iCommandPort, iMulticastPort;
	theServer->GetSocketInfo(szDataIP, &iDataPort, szCommandIP, &iCommandPort, szMulticastGroup, &iMulticastPort);

	//strcpy(szMulticastGroup, "255.255.255.255"); //R

	printf("Command Socket  : %s:%d\n", szCommandIP, iCommandPort);
	printf("Data Socket     : %s:%d\n", szDataIP, iDataPort);
	if (iConnectionType == ConnectionType_Multicast)
	{
		printf("Connection Type : Multicast\n");
		printf("Multicast Group : %s:%d\n", szMulticastGroup, iMulticastPort);
	}
	else
		printf("Connection Type : Unicast\n");


	return ErrorCode_OK;

}

// RequestHandler receives requests from the Client.  Server should
// build and return "request responses" packets in this same thread.
//
//   pPacketIn  - data packet from Client
//   pPacketOut - empty packet, to be filled in and returned to the
//				  Client as the "request response".
//
int __cdecl RequestHandler(sPacket* pPacketIn, sPacket* pPacketOut, void* pUserData)
{
	int iHandled = 1;	// handled

	printf("[SampleServer] ** Determining handling of request %d\n", pPacketIn->iMessage);

	switch (pPacketIn->iMessage)
	{
	case NAT_PING:
		printf("[SampleServer] received ping from Client.\n");
		printf("[SampleServer] Client App Name : %s\n", pPacketIn->Data.Sender.szName);
		printf("[SampleServer] Client App Version : %d.%d.%d.%d\n", pPacketIn->Data.Sender.Version[0],
			pPacketIn->Data.Sender.Version[1], pPacketIn->Data.Sender.Version[2], pPacketIn->Data.Sender.Version[3]);
		printf("[SampleServer] Client App NatNet Version : %d.%d.%d.%d\n", pPacketIn->Data.Sender.NatNetVersion[0],
			pPacketIn->Data.Sender.NatNetVersion[1], pPacketIn->Data.Sender.NatNetVersion[2], pPacketIn->Data.Sender.NatNetVersion[3]);

		// build server info packet
		strcpy(pPacketOut->Data.Sender.szName, "SimpleServer");
		pPacketOut->Data.Sender.Version[0] = 2;
		pPacketOut->Data.Sender.Version[1] = 1;
		pPacketOut->iMessage = NAT_PINGRESPONSE;
		pPacketOut->nDataBytes = sizeof(sSender);
		iHandled = 1;
		break;

	case NAT_REQUEST_MODELDEF:
		printf("[SimpleServer] Received request for data descriptions.\n");
		theServer->PacketizeDataDescriptions(&descriptions, pPacketOut);
		break;

	case NAT_REQUEST_FRAMEOFDATA:
	{
		// note: Client does not typically poll for data, but we accomodate it here anyway
		// note: need to return response on same thread as caller
		printf("[SimpleServer] Received request for frame of data.\n");
		sFrameOfMocapData frame;
		BuildFrame(g_lCurrentFrame, &descriptions, &frame);
		theServer->PacketizeFrameOfMocapData(&frame, pPacketOut);
		FreeFrame(&frame);

		//BuildFrame(g_lCurrentFrame, &descriptions, &frameToSend);
		//theServer->PacketizeFrameOfMocapData(&frameToSend, pPacketOut);

	}
	break;

	case NAT_REQUEST:
		printf("[SampleServer] Received request from Client: %s\n", pPacketIn->Data.szData);
		pPacketOut->iMessage = NAT_UNRECOGNIZED_REQUEST;
		if (stricmp(pPacketIn->Data.szData, "TestRequest") == 0)
		{
			pPacketOut->iMessage = NAT_RESPONSE;
			strcpy(pPacketOut->Data.szData, "TestResponse");
			pPacketOut->nDataBytes = ((int)strlen(pPacketOut->Data.szData)) + 1;
		}
		break;

	default:
		pPacketOut->iMessage = NAT_UNRECOGNIZED_REQUEST;
		pPacketOut->nDataBytes = 0;
		iHandled = 0;
		break;
	}

	return iHandled; // 0 = not handled, 1 = handled;
}

// Build a DataSet description (MarkerSets, RigiBodies, Skeletons).
void BuildDescription(sDataDescriptions* pDescription)
{
	printf("Building (dummy) data descriptions..");

	pDescription->nDataDescriptions = 0;
	int index = 0;

#if STREAM_MARKERS
	// Marker Set Description
	sMarkerSetDescription* pMarkerSetDescription = new sMarkerSetDescription();
	strcpy(pMarkerSetDescription->szName, "Katie");
	pMarkerSetDescription->nMarkers = 10;
	pMarkerSetDescription->szMarkerNames = new char*[pMarkerSetDescription->nMarkers];
	char szTemp[128];
	for (int i = 0; i < pMarkerSetDescription->nMarkers; i++)
	{
		sprintf(szTemp, "Marker %d", i);
		pMarkerSetDescription->szMarkerNames[i] = new char[MAX_NAMELENGTH];
		strcpy(pMarkerSetDescription->szMarkerNames[i], szTemp);
	}
	pDescription->arrDataDescriptions[index].type = Descriptor_MarkerSet;
	pDescription->arrDataDescriptions[index].Data.MarkerSetDescription = pMarkerSetDescription;
	pDescription->nDataDescriptions++;
	index++;
#endif

#if STREAM_RBS
	// Rigid Body Description
	for (int i = 0; i < 20; i++)
	{
		sRigidBodyDescription* pRigidBodyDescription = new sRigidBodyDescription();
		sprintf(pRigidBodyDescription->szName, "RigidBody %d", i);
		pRigidBodyDescription->ID = i;
		pRigidBodyDescription->offsetx = 1.0f;
		pRigidBodyDescription->offsety = 2.0f;
		pRigidBodyDescription->offsetz = 3.0f;
		pRigidBodyDescription->parentID = 2;
		pDescription->arrDataDescriptions[index].type = Descriptor_RigidBody;
		pDescription->arrDataDescriptions[index].Data.RigidBodyDescription = pRigidBodyDescription;
		pDescription->nDataDescriptions++;
		index++;
	}
#endif

#if STREAM_SKELETONS
	// Skeleton description
	for (int i = 0; i < 2; i++)
	{
		sSkeletonDescription* pSkeletonDescription = new sSkeletonDescription();
		sprintf(pSkeletonDescription->szName, "Skeleton %d", i);
		pSkeletonDescription->skeletonID = index * 100;
		pSkeletonDescription->nRigidBodies = 5;
		for (int j = 0; j < pSkeletonDescription->nRigidBodies; j++)
		{
			int RBID = pSkeletonDescription->skeletonID + j;
			sprintf(pSkeletonDescription->RigidBodies[j].szName, "RB%d", RBID);
			pSkeletonDescription->RigidBodies[j].ID = RBID;
			pSkeletonDescription->RigidBodies[j].offsetx = 1.0f;
			pSkeletonDescription->RigidBodies[j].offsety = 2.0f;
			pSkeletonDescription->RigidBodies[j].offsetz = 3.0f;
			pSkeletonDescription->RigidBodies[j].parentID = 2;
		}
		pDescription->arrDataDescriptions[index].type = Descriptor_Skeleton;
		pDescription->arrDataDescriptions[index].Data.SkeletonDescription = pSkeletonDescription;
		pDescription->nDataDescriptions++;
		index++;
	}
#endif

	printf("done\n");
}

// Send DataSet description to Client
void SendDescription(sDataDescriptions* pDescription)
{
	sPacket packet;
	theServer->PacketizeDataDescriptions(pDescription, &packet);
	theServer->SendPacket(&packet);
}

// Release any memory we allocated for our example
void FreeDescription(sDataDescriptions* pDescription)
{
	/*
	for(int i=0; i< pDescription->nDataDescriptions; i++)
	{
	if(pDescription->arrDataDescriptions[i].type == Descriptor_MarkerSet)
	{
	for(int i=0; i< pMarkerSetDescription->nMarkers; i++)
	{
	delete[] pMarkerSetDescription->szMarkerNames[i];
	}
	delete elete[] pMarkerSetDescription->szMarkerNames;
	sMarkerSetDescription* pMarkerSetDescription = pDescription->arrDataDescriptions[0].Data.MarkerSetDescription;
	}
	else if(pDescription->arrDataDescriptions[i].type == Descriptor_RigidBody)
	{
	*/

}

// Build frame of MocapData
void BuildFrame(long FrameNumber, sDataDescriptions* pModels, sFrameOfMocapData* pOutFrame)
{
	if (!pModels)
	{
		printf("No models defined - nothing to send.\n");
		return;
	}

	ZeroMemory(pOutFrame, sizeof(sFrameOfMocapData));
	pOutFrame->iFrame = FrameNumber;
	pOutFrame->fLatency = (float)GetTickCount();
	pOutFrame->nOtherMarkers = 0;
	pOutFrame->nMarkerSets = 0;
	pOutFrame->nRigidBodies = 0;
	pOutFrame->nLabeledMarkers = 0;

	for (int i = 0; i < pModels->nDataDescriptions; i++)
	{
#if STREAM_MARKERS
		// MarkerSet data
		if (pModels->arrDataDescriptions[i].type == Descriptor_MarkerSet)
		{
			// add marker data
			int index = pOutFrame->nMarkerSets;
			sMarkerSetDescription* pMS = pModels->arrDataDescriptions[i].Data.MarkerSetDescription;
			strcpy(pOutFrame->MocapData[index].szName, pMS->szName);
			pOutFrame->MocapData[index].nMarkers = pMS->nMarkers;
			pOutFrame->MocapData[index].Markers = new MarkerData[pOutFrame->MocapData[0].nMarkers];
			for (int iMarker = 0; iMarker < pOutFrame->MocapData[index].nMarkers; iMarker++)
			{
				double rads = (double)counter * 3.14159265358979 / 180.0f;
				pOutFrame->MocapData[index].Markers[iMarker][0] = (float)sin(rads) + (10 * iMarker);		// x
				pOutFrame->MocapData[index].Markers[iMarker][1] = (float)cos(rads) + (20 * iMarker);		// y
				pOutFrame->MocapData[index].Markers[iMarker][2] = (float)tan(rads) + (30 * iMarker);		// z
				counter++;
				counter %= 360;
			}
			pOutFrame->nMarkerSets++;

		}
#endif

#if STREAM_RBS
		// RigidBody data
		if (pModels->arrDataDescriptions[i].type == Descriptor_RigidBody)
		{
			sRigidBodyDescription* pMS = pModels->arrDataDescriptions[i].Data.RigidBodyDescription;
			int index = pOutFrame->nRigidBodies;
			sRigidBodyData* pRB = &pOutFrame->RigidBodies[index];

			pRB->ID = pMS->ID;
			double rads = (double)counter * 3.14159265358979 / 180.0f;
			pRB->x = (float)sin(rads);
			pRB->y = (float)cos(rads);
			pRB->z = (float)tan(rads);

			EulerAngles ea;
			ea.x = (float)sin(rads);
			ea.y = (float)cos(rads);
			ea.z = (float)tan(rads);
			ea.w = 0.0f;
			Quat q = Eul_ToQuat(ea);
			pRB->qx = q.x;
			pRB->qy = q.y;
			pRB->qz = q.z;
			pRB->qw = q.w;

			pRB->nMarkers = 3;
			pRB->Markers = new MarkerData[pRB->nMarkers];
			pRB->MarkerIDs = new int[pRB->nMarkers];
			pRB->MarkerSizes = new float[pRB->nMarkers];
			pRB->MeanError = 0.0f;
			for (int iMarker = 0; iMarker < pRB->nMarkers; iMarker++)
			{
				pRB->Markers[iMarker][0] = iMarker + 0.1f;		// x
				pRB->Markers[iMarker][1] = iMarker + 0.2f;		// y
				pRB->Markers[iMarker][2] = iMarker + 0.3f;		// z
				pRB->MarkerIDs[iMarker] = iMarker + 200;
				pRB->MarkerSizes[iMarker] = 77.0f;
			}
			pOutFrame->nRigidBodies++;
			counter++;

		}
#endif

#if STREAM_SKELETONS
		// Skeleton data
		if (pModels->arrDataDescriptions[i].type == Descriptor_Skeleton)
		{
			sSkeletonDescription* pSK = pModels->arrDataDescriptions[i].Data.SkeletonDescription;
			int index = pOutFrame->nSkeletons;

			pOutFrame->Skeletons[index].skeletonID = pSK->skeletonID;
			pOutFrame->Skeletons[index].nRigidBodies = pSK->nRigidBodies;
			// RigidBody data
			pOutFrame->Skeletons[index].RigidBodyData = new sRigidBodyData[pSK->nRigidBodies];
			for (int i = 0; i < pSK->nRigidBodies; i++)
			{
				sRigidBodyData* pRB = &pOutFrame->Skeletons[index].RigidBodyData[i];
				pRB->ID = pOutFrame->Skeletons[index].skeletonID + i;
				double rads = (double)counter * 3.14159265358979 / 180.0f;
				pRB->x = (float)sin(rads);
				pRB->y = (float)cos(rads);
				pRB->z = (float)tan(rads);
				fCounter += 0.1f;
				if (fCounter > 1.0f)
					fCounter = 0.1f;
				pRB->qx = fCounter;
				pRB->qy = fCounter + 0.1f;
				pRB->qz = fCounter + 0.2f;
				pRB->qw = 1.0f;
				pRB->nMarkers = 3;
				pRB->Markers = new MarkerData[pRB->nMarkers];
				pRB->MarkerIDs = new int[pRB->nMarkers];
				pRB->MarkerSizes = new float[pRB->nMarkers];
				pRB->MeanError = 0.0f;
				for (int iMarker = 0; iMarker < pRB->nMarkers; iMarker++)
				{
					pRB->Markers[iMarker][0] = iMarker + 0.1f;		// x
					pRB->Markers[iMarker][1] = iMarker + 0.2f;		// y
					pRB->Markers[iMarker][2] = iMarker + 0.3f;		// z
					pRB->MarkerIDs[iMarker] = iMarker + 200;
					pRB->MarkerSizes[iMarker] = 77.0f;
				}
				counter++;
			}

			pOutFrame->nSkeletons++;

		}
#endif
	}

#if STREAM_LABELED_MARKERS
	// add marker data
	pOutFrame->nLabeledMarkers = 10;
	for (int iMarker = 0; iMarker < 10; iMarker++)
	{
		sMarker* pMarker = &pOutFrame->LabeledMarkers[iMarker];
		pMarker->ID = iMarker + 100;
		pMarker->x = (float)iMarker;
		pMarker->y = (float)(counter2 * iMarker);
		pMarker->z = (float)iMarker;
		pMarker->size = 5.0f;
	}
	counter2++;
	counter2 %= 100;
#endif

}

// Packetize and send a single frame of mocap data to the Client
void SendFrame(sFrameOfMocapData* pFrame)
{
	sPacket packet;
	theServer->PacketizeFrameOfMocapData(pFrame, &packet);
	theServer->SendPacket(&packet);
	
	if (pFrame->iFrame % 10 == 0)
		printFrameSend("%d", pFrame->iFrame); //g_lCurrentFrame);
	else
		printFrameSend(".", pFrame->iFrame); //g_lCurrentFrame);
}

// Release any memory we allocated for this sample
void FreeFrame(sFrameOfMocapData* pFrame)
{
	for (int i = 0; i < pFrame->nMarkerSets; i++)
	{
		delete[] pFrame->MocapData[i].Markers;
	}

	for (int i = 0; i < pFrame->nOtherMarkers; i++)
	{
		delete[] pFrame->OtherMarkers;
	}

	for (int i = 0; i < pFrame->nRigidBodies; i++)
	{
		delete[] pFrame->RigidBodies[i].Markers;
		delete[] pFrame->RigidBodies[i].MarkerIDs;
		delete[] pFrame->RigidBodies[i].MarkerSizes;
	}

}

// PlayingThread_Func streams data at ~60hz 
DWORD WINAPI PlayingThread_Func(void * empty)
{
	bool keepSend = true;
	while (keepSend)
	{
		//sFrameOfMocapData frame;
		FreeFrame(&frameToSend);


		//BuildFrame(g_lCurrentFrame, &descriptions, &frame);
		//SendFrame(&frame);
		//FreeFrame(&frame);
		BuildFrame(g_lCurrentFrame, &descriptions, &frameToSend);
		SendFrame(&frameToSend);


		g_lCurrentFrame++;
		HiResSleep(FRAME_PLAY_THREAD_DELAY);
		 
		keepSend = isContinuousSendEnabled;
	}

	return ErrorCode_OK;
}

int StartPlayingThread()
{
	SECURITY_ATTRIBUTES security_attribs;
	security_attribs.nLength = sizeof(SECURITY_ATTRIBUTES);
	security_attribs.lpSecurityDescriptor = NULL;
	security_attribs.bInheritHandle = TRUE;

	PlayingThread_Handle = CreateThread(&security_attribs, 0, PlayingThread_Func, NULL, 0, &PlayingThread_ID);
	if (PlayingThread_Handle == NULL)
	{
		printf("Error creating play thread.");
		return -1;
	}
	else
		g_bPlaying = true;

	return ErrorCode_OK;
}

int StopPlayingThread()
{
	if (PlayingThread_Handle != NULL)
		TerminateThread(PlayingThread_Handle, 0);

	PlayingThread_Handle = NULL;
	g_bPlaying = false;

	return ErrorCode_OK;
}

void resetServer()
{
	int iSuccess = 0;
	char szIPAddress[128];
	theServer->IPAddress_LongToString(lAddresses[0], szIPAddress);	// use first IP detected

	printf("\nRe-initting Server\n\n");

	iSuccess = theServer->Uninitialize();
	if (iSuccess != 0)
		printf("Error uninitting server.\n");

	iSuccess = theServer->Initialize(szIPAddress);
	if (iSuccess != 0)
		printf("Error re-initting server.\n");

}

// MessageHandler receives NatNet error mesages
void __cdecl MessageHandler(int msgType, char* msg)
{
	printf("\n%s\n", msg);
}

// higer res sleep than standard.
int HiResSleep(int msecs)
{
	HANDLE hTempEvent = CreateEvent(0, true, FALSE, _T("TEMP_EVENT"));
	timeSetEvent(msecs, 1, (LPTIMECALLBACK)hTempEvent, 0, TIME_ONESHOT | TIME_CALLBACK_EVENT_SET);
	WaitForSingleObject(hTempEvent, msecs);
	CloseHandle(hTempEvent);
	return 1;
}
