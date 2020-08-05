args <- commandArgs(trailingOnly = T)

n = length(args)
message(paste("n = ", n, sep=""))
#Sys.sleep(100)

if ( n >= 1 ) {
	filename = args[1]
	message(paste("filename = ", filename, sep=""))
}else
{
	exit()
}
#Sys.sleep(100)


df <- read.csv( filename, header=T, stringsAsFactors = F, na.strings = c("", "NA"))

sink(file = "header.txt")
n <- length(names(df))
for ( i in 1:n){
	cat(names(df)[i])
	cat("\n")
}
sink()
#Sys.sleep(100)
