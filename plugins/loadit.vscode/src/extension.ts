'use strict';

import * as vscode from 'vscode';
const TEST_REGEX = /await Execute<.*>\.Run\(\);/;

let controller: TestController = null;
function registerRunTestsCommands (context:vscode.ExtensionContext){
    context.subscriptions.push(
        vscode.commands.registerCommand('loadit.runTestsFile', () => {
            controller.runTests(context);
        })
    );
}

function registerRunTestFileCommands (context:vscode.ExtensionContext){
    context.subscriptions.push(
        vscode.commands.registerCommand('loadit.runTestFile', args => {
            controller.startTestRun(args.fsPath);
        })
    );
}

export function activate(context:vscode.ExtensionContext) {
    controller = new TestController();
    registerRunTestsCommands(context);
    registerRunTestFileCommands(context);
    context.subscriptions.push(controller);
}

// this method is called when your extension is deactivated
export function deactivate() {
}

class TestController {
    lastFile:string;
    public runTests(context:vscode.ExtensionContext) {
        let editor = vscode.window.activeTextEditor;

        if (!editor)
            return;

        let doc = editor.document;

        if (doc.languageId !== "csharp")
            return;

        var document = editor.document;
        var text = document.getText();
        if(this.hasTest(text)) {
            this.startTestRun(document.fileName);
        } else {
            vscode.window.showErrorMessage(`No tests found. Make sure your file contains the await Execute<Test4>.Run(); code`);
        }
    }

    private hasTest(text): boolean {
        var match = TEST_REGEX.exec(text);
        return match !== null;
    }
    
    public startTestRun(filePath:string) {
        if (!filePath) {
            vscode.window.showErrorMessage(`No tests found. Make sure your file contains the await Execute<Test4>.Run(); code`);
            return;
        }
        this.lastFile = filePath;
        const terminal = vscode.window.createTerminal(`Load it runner`);
        terminal.sendText("loadit --run --file " + filePath);
        terminal.show();        
    }

    dispose() {

    }
}