pipeline {
    agent any

    options {
        timeout(time: 30, unit: 'MINUTES')
        buildDiscarder(logRotator(numToKeepStr: '10'))
        disableConcurrentBuilds()
    }

    environment {
        DOTNET_CLI_TELEMETRY_OPTOUT = '1'
        NEXT_TELEMETRY_DISABLED     = '1'
        JENKINS_CONTAINER           = 'homematch-jenkins'
    }

    stages {
        stage('Checkout Code') {
            steps {
                echo 'Checking out source code from repository...'
                checkout scm
            }
        }

        stage('Backend Restore & Build') {
            steps {
                echo 'Restoring dependencies & building ASP.NET Core (.NET 10)...'
                sh '''
                    docker run --rm \
                        --volumes-from ${JENKINS_CONTAINER} \
                        -w "${WORKSPACE}" \
                        mcr.microsoft.com/dotnet/sdk:10.0 \
                        sh -c "dotnet restore backend/HomeMatch.sln && dotnet build backend/HomeMatch.sln --no-restore --configuration Release"
                '''
            }
        }

        stage('Backend Tests') {
            steps {
                echo 'Running Backend Unit & Integration Tests...'
                sh '''
                    docker run --rm \
                        --volumes-from ${JENKINS_CONTAINER} \
                        -w "${WORKSPACE}" \
                        mcr.microsoft.com/dotnet/sdk:10.0 \
                        sh -c "dotnet test backend/HomeMatch.sln --no-build --configuration Release --verbosity normal --logger 'trx;LogFileName=test_results.trx'"
                '''
            }
        }

        stage('Frontend Build & Lint') {
            steps {
                echo 'Installing dependencies & building Next.js 15 (Node 22)...'
                sh '''
                    docker run --rm \
                        --volumes-from ${JENKINS_CONTAINER} \
                        -w "${WORKSPACE}/frontend" \
                        node:22-alpine \
                        sh -c "npm ci || npm install && npm run build"
                '''
            }
        }

        stage('Validate Docker Compose') {
            steps {
                echo 'Validating Docker Compose configuration...'
                sh 'docker compose config'
            }
        }
    }

    post {
        always {
            echo 'Pipeline execution completed.'
            cleanWs deleteDirs: true, notFailBuild: true
        }
        success {
            echo 'CI Pipeline Passed: All checks, tests, and builds succeeded!'
        }
        failure {
            echo 'CI Pipeline Failed: One or more stages failed. Check logs above.'
        }
    }
}
