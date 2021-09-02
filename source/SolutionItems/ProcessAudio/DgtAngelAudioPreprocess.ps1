Clear-Host
Write-Output "         ___            _ _        ______                                "                      
Write-Output "        / _ \          | (_)       | ___ \                               "
Write-Output "       / /_\ \_   _  __| |_  ___   | |_/ / __ ___   ___ ___ ___  ___ ___ "
Write-Output "       |  _  | | | |/ _  | |/ _ \  |  __/ '__/ _ \ / __/ __/ _ \/ __/ __|"
Write-Output "       | | | | |_| | (_| | | (_) | | |  | | | (_) | (_| (_|  __/\__ \__ \"
Write-Output "       \_| |_/\__,_|\__,_|_|\___/  \_|  |_|  \___/ \___\___\___||___/___/"
Write-Output "                                      Values for Audio Source - ClipTools"

# OPTIONS@ https://ffmpeg.org/ffmpeg-filters.html#silencedetect

$sParams = "start_periods=600",
"start_duration=0",
"start_silence=0",
"start_threshold=-40dB",
"start_mode=any",
"stop_periods=1",
"stop_duration=0.2",
#"stop_silence=0",
"stop_threshold=-50dB",
"stop_mode=any",
"window=0.02",
"detection=rms"

$silenceremove = "silenceremove=" + ($sParams -Join ":")
#$silenceremove =  "silenceremove=" + $silenceremove

Write-Output "--------------------------------------------------------------------------------"
Write-Output "Audio preprocessor for DGT Angel"
Write-Output "Ffmpeg Options $silenceremove"
Write-Output ""
Write-Output ">>Processing Audio"

$files = Get-ChildItem ".\*.wav"
#$files = Get-ChildItem ".\Letters-A.wav"

foreach ($f in $files) {
    $infile = $f.FullName 
    $outfile = $f.FullName.Replace(".wav", "-AP.wav") 
    
    Write-Output ">>Processing $infile to $outfile"
    .\ffmpeg -hide_banner -loglevel warning -y -i "$infile" -ac 2 -af  "$silenceremove" "$outfile"

}

Write-Output ">>Processing Complete"
Write-Output "--------------------------------------------------------------------------------"



