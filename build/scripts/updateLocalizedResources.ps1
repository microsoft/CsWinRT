# ------------------------------------------------------------------------------------------------
# - Script to Create a pull request using Personal Access token
# ------------------------------------------------------------------------------------------------

$teamProject = $env:SYSTEM_TEAMPROJECT
$repoName= $env:Build_Repository_Name
$pipelineName = $env:Build_DefinitionName
$PAT = $env:CsWinRTPullRequestPAT

Write-host "Azure DevOps organization: $env:System_TeamFoundationCollectionUri"
Write-host "Team Project: $teamProject"
Write-host "Repo Name: $repoName"
Write-host "Source Branch: $(Build.SourceBranch)"


if ("$(git status)".Contains("nothing to commit")) {
    Write-Host "Nothing to commit, exiting immediately"
    Exit
}

#*****************************************
#************Configure GIT***************
#*****************************************
Write-Host "##[group] Configure GIT"

#Setting Global Git Username and Email. NOTE: Branch will be created as this user
git config --global user.email "jlarkin@microsoft.com"
git config --global user.name "jlarkin"

#Set git password. NOTE: PAT Personal Access token for user to create Pull Request or commit
$encodedPat = [System.Convert]::ToBase64String([System.Text.Encoding]::UTF8.GetBytes(":$(PAT)"))

$gitURI = "$(Build.Repository.Uri)"
git config "http.$gitURI.extraheader AUTHORIZATION: Basic $encodedPat"

Write-Host "##[endgroup]"

#*****************************************
#***Create Branch and push changes***
#*****************************************

Write-Host "##[group] Create Branch and push changes"
#Create Branch and add changes

Write-Host "Creating Branch" 

$d = (Get-Date).ToString("yyyyMMdd-hhmm")
$branchName = "tdbuild/$d"
#Write-Host "##vso[task.setvariable variable=branchName;]$branchName"

git checkout -b $branchName
git push --set-upstream origin $branchName
git add -A
git commit -m "TDBuild latest localization" -a
git push

Write-host "Created Branch: $branchName"
Write-Host "##[endgroup]"

#*****************************************
#******Create Pull request changes*****
#*****************************************
Write-Host "##[group] Create Pull Request"
Write-Host "Creating Pull Request" 

$sourceBranch ="refs/heads/$branchName"
$targetBranch= $env:BUILD_SOURCEBRANCH

$body = @{
    sourceRefName = "$sourceBranch"
    targetRefName = "$targetBranch"
    title = "Latest loc from $pipelineName"
    description = "Latest loc from $pipelineName"
}

$jsonBody = ConvertTo-Json $body

$url = "$env:System_TeamFoundationCollectionUri/$teamProject/_apis/git/repositories/$repoName/pullrequests?api-version=4.0"

$authHeader = @{Authorization = "Basic $encodedPat"}

$prResult = Invoke-RestMethod -Uri $url -Method Post -Headers $authHeader -Body $jsonBody -ContentType "application/json;charset=UTF-8"

$prId = $prResult.pullRequestId

Write-Host "##[endgroup]"

Write-Host "Link to PR: $gitURI/pullrequest/$prId"
