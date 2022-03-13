import * as path from 'path';
import * as vscode from 'vscode';
import * as fs from 'fs';

interface StructuredTextBuilderTaskDefinition extends vscode.TaskDefinition
{
    folder: string;
}

function exists(file: string): Promise<boolean> {
	return new Promise<boolean>((resolve, _reject) => {
		fs.exists(file, (value) => {
			resolve(value);
		});
	});
}

export class StructuredTextTaskProvider implements vscode.TaskProvider
{
    static Type = "structured-text";
    static BuildTask = "build";
    private _compilerPath : string;
    constructor(compilerPath : string)
    {
        this._compilerPath = compilerPath;
    }
    provideTasks(token: vscode.CancellationToken): vscode.ProviderResult<vscode.Task[]>
    {
        return getTasks(this._compilerPath);

    }
    resolveTask(task: vscode.Task, token: vscode.CancellationToken): vscode.ProviderResult<vscode.Task> {
        if(task.definition.type !== StructuredTextTaskProvider.Type) {
            return undefined;
        }

        return resolveTask(this._compilerPath, task);
    }
}

async function resolveTask(compilerPath : string, task: vscode.Task) : Promise<vscode.Task|undefined>
{
    const definition = <StructuredTextBuilderTaskDefinition>task.definition;
    return getTask(compilerPath, definition.folder, definition);
}

function getTask(compilerPath : string, folder : string, definition?: StructuredTextBuilderTaskDefinition) :vscode.Task
{
    if(definition === undefined)
    {
        definition = {
            type: StructuredTextTaskProvider.Type,
            name: "build",
            folder: folder
        };
    }

    const processExecution = new vscode.ProcessExecution(compilerPath, ["--folder", definition.folder]);
    const buildTask = new vscode.Task(
        definition, // taskDefinition
        vscode.TaskScope.Workspace, // workspaceFolder
        "build", // name
        StructuredTextTaskProvider.Type, // source
        processExecution, // execution
        "$problem-matcher.structured-text"); // problem matcher
    buildTask.group = vscode.TaskGroup.Build;
    return buildTask;
}

async function getTasks(compilerPath : string): Promise<vscode.Task[]>
{
    const editor = vscode.window.activeTextEditor;
    const emptyTasks: vscode.Task[] = [];
    if(!editor) {
        return emptyTasks;
    }
    const fileExt : string = path.extname(editor.document.fileName);
    if(!fileExt) {
        return emptyTasks;
    }
    if(!fileExt.endsWith(".st")) {
        return emptyTasks;
    }

    var folder = path.dirname(editor.document.fileName)
    var buildTask = getTask(compilerPath, folder, undefined);
    return [buildTask];
}