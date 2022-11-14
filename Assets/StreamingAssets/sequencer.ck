// number of steps (MUST match the Unity NUM_STEPS!)
24 => int NUM_STEPS;
// number of tracks (MUST match the Unity NUM_TRACKS!)
4 => int NUM_TRACKS;
// number of trees (MUST match the Unity NUM_TREES!)
3 => int NUM_TREES;
// beat (smallest division; dur between chickens)
400::ms => dur BEAT;
// update rate for pos
10::ms => dur POS_RATE;
// increment per step
POS_RATE/BEAT => float posInc;

// global (outgoing) variables for Unity animation
global int currentStep; // which step we are at
// will take on fractional values for smooth animation[0-NUM_STEPS)
global float playheadPos;

// global (incoming) data from Unity
global int editWhichTrack;
global int editWhichStep;
1 => global float editRate;
1 => global float editGain;
0 => global int editInstrument;
0 => global int editWhichTree;
0 => global int editTreeStatus;
1 => global float editTempo;
global Event editHappened;

// a sequence (one of many ways to "sequence")
float seqRate[NUM_TRACKS][NUM_STEPS];
float seqGain[NUM_TRACKS][NUM_STEPS];
int treeStatus[NUM_TREES];
int nextStep[NUM_STEPS];
int instruments[NUM_TRACKS][NUM_TREES];
// initialize
for( int i; i < NUM_TRACKS; i++ )
{
    for( int j; j < NUM_STEPS; j++ )
    {
        0 => seqGain[i][j];
        1 => seqRate[i][j];
    }
}
for( int i; i < NUM_STEPS; i++ )
{
    i + 1 => nextStep[i];
    if( nextStep[i] >= NUM_STEPS ) 0 => nextStep[i];
}


// sound buffers
SndBuf bufs[NUM_TRACKS][NUM_STEPS];
// reverb
NRev reverb => dac;
// reverb mix
.1 => reverb.mix;

// connect them
for( int i; i < NUM_TRACKS; i++ )
{
    for( int j; j < NUM_STEPS; j++ )
    {
         // connect to dac
         bufs[i][j] => reverb;
         // load sound
         if( i == 0 ) me.dir() + "bell.wav" => bufs[i][j].read;
         else if( i == 1 ) me.dir() + "pianoHigh.wav" => bufs[i][j].read;
         else if( i == 2 ) me.dir() + "pianoLow.wav" => bufs[i][j].read;
         else me.dir() + "drum.wav" => bufs[i][j].read;
         // silence
         0 => bufs[i][j].gain;
    }
}

// spork update
spork ~ playheadPosUpdate();
// spork edit listener
spork ~ listenForEdit();

// simple sequencer loop
while( true )
{
    // play current
    for( int i; i < NUM_TRACKS; i++ )
    {
        play( i, currentStep, seqGain[i][currentStep], seqRate[i][currentStep] );
    }
    // sync with discrete grid position
    currentStep => playheadPos;
    // advance time by duration of one beat 
    BEAT => now;
    // increment to next ornament
    nextStep[currentStep] => currentStep;
}

fun void play( int whichTrack, int whichStep, float gain, float rate )
{
    // restart
    0 => bufs[whichTrack][whichStep].pos;
    // set gain
    gain => bufs[whichTrack][whichStep].gain;
    // set rate
    rate => bufs[whichTrack][whichStep].rate;
}

// updates the global playheadPos with fine granularity,
// for visualizing the playhead smoothly in Unity
fun void playheadPosUpdate()
{
    while( true )
    {
        // increment
        posInc +=> playheadPos;
        // advance time
        POS_RATE => now;
    }
}


// this listens for events from Unity to update the sequence
fun void listenForEdit()
{
    while( true )
    {
        // wait for event
        editHappened => now;
        // update the gain, rate, and tree on/off status
        editRate => seqRate[editWhichTrack][editWhichStep];
        editGain => seqGain[editWhichTrack][editWhichStep];
        editTreeStatus => treeStatus[editWhichTree];
        // update the instruments
        "" => string filename;
        if( editInstrument == 0 ) "bell.wav" => filename;
        else if( editInstrument == 1 ) "pianoHigh.wav" => filename;
        else if( editInstrument == 2 ) "pianoLow.wav" => filename;
        else "drum.wav" => filename;
        // reconnect SndBuf to new sound file
        for( int j; j < 8; j++ ) 
        {
            me.dir() + filename => bufs[editWhichTrack][editWhichTree * 8 + j].read;
            0 => bufs[editWhichTrack][editWhichTree * 8 + j].gain;
        }
        // adjust tempo
        (editTempo * 400)::ms => BEAT;
        // hopefully this will skip trees without any ornaments???
        if( treeStatus[0] == 0 && treeStatus[1] == 0 && treeStatus[2] == 0) 
        {
            for( int i; i < 24; i++ ) 0 => nextStep[i];
        }
        else if( treeStatus[0] > 0 && treeStatus[1] == 0 && treeStatus[2] == 0 )
        {
            for( int i; i < 7; i++ ) i + 1 => nextStep[i];
            for( int i; i < 17; i++ ) 0 => nextStep[i + 7];
        }
        else if( treeStatus[0] == 0 && treeStatus[1] > 0 && treeStatus[2] == 0 )
        {
            for( int i; i < 7; i++ ) i + 9 => nextStep[i + 8];
            for( int i; i < 8; i++ ) 8 => nextStep[i];
            for( int i; i < 9; i++ ) 8 => nextStep[i + 15];
        }
        else if( treeStatus[0] == 0 && treeStatus[1] == 0 && treeStatus[2] > 0 )
        {
            for( int i; i < 7; i++ ) i + 17 => nextStep[i + 16];
            for( int i; i < 16; i++ ) 16 => nextStep[i];
        }
        else if( treeStatus[0] > 0 && treeStatus[1] > 0 && treeStatus[2] == 0 )
        {
            for( int i; i < 15; i++ ) i + 1 => nextStep[i];
            for( int i; i < 9; i++ ) 0 => nextStep[i + 15];
        }
        else if( treeStatus[0] > 0 && treeStatus[1] == 0 && treeStatus[2] > 0 )
        {
            for( int i; i < 7; i++ ) i + 1 => nextStep[i];
            for( int i; i < 9; i++ ) 16 => nextStep[i + 7];
            for( int i; i < 7; i++ ) i + 17 => nextStep[i + 16];
            0 => nextStep[23];
        }
        else if( treeStatus[0] == 0 && treeStatus[1] > 0 && treeStatus[2] > 0 )
        {
            for( int i; i < 8; i++ ) 8 => nextStep[i];
            for( int i; i < 15; i++ ) i + 9 => nextStep[i + 8];
            8 => nextStep[23];
        }
        else if( treeStatus[0] > 0 && treeStatus[1] > 0 && treeStatus[2] > 0 )
        {
            for( int i; i < 23; i++ ) i + 1 => nextStep[i];
            0 => nextStep[23];
        }
    }
}