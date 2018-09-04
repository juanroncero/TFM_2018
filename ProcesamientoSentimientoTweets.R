library(RODBC)
library(googleLanguageR)

setwd("c:\\Master ML\\R\\")

## AUTENTICACIÓN API
gl_auth('xxx.json')

## ACCESO BASE DE DATOS
dbconnection <- odbcDriverConnect("driver={SQL Server};server=x.x.x.x;database=CRYPTOCOIN;uid=criptocoin;pwd=pepe; trusted_connection=yes")
tweets <- sqlQuery(dbconnection,paste("SELECT * FROM [CRYPTOCOIN].[dbo].[TWEETS] ORDER BY ID DESC"))

## API GOOGLE SENTIMIENTO
tweets$TWEET = lapply(tweets$TWEET, as.character)
sentimiento <- lapply(tweets$TWEET, function(t) gl_nlp(t))
tweets$PUNTUACIONES <- sapply(sentimiento, function(t) t$documentSentiment$score)


procesa <- function(row){
  id = row$ID
  sentimiento = row$PUNTUACIONES
  queryAct = paste("UPDATE [CRYPTOCOIN].[dbo].[TWEETS] SET SENTIMIENTO =", sentimiento, " WHERE ID = ",id)
  sqlQuery(dbconnection,paste(queryAct))
  #print(queryAct)
}

by(tweets, 1:nrow(tweets), function(row) procesa(row))

