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
    provideTasks(token: vscode.CancellationToken): vscode.ProviderResult<vscode.Task[]>
    {
        return getTasks();

    }
    resolveTask(task: vscode.Task, token: vscode.CancellationToken): vscode.ProviderResult<vscode.Task> {
        if(task.definition.type !== StructuredTextTaskProvider.Type) {
            return undefined;
        }

        return resolveTask(task);
    }
}

async function resolveTask(task: vscode.Task) : Promise<vscode.Task|undefined>
{
    const definition = <StructuredTextBuilderTaskDefinition>task.definition;
    const compilerPath : string = "C:\\Home\\source\\Iec361131\\Projects\\OfflineCompiler\\bin\\Debug\\net5.0\\OfflineCompiler.exe";
    if(!(await exists(compilerPath))) {
        return undefined;
    }
    return getTask(definition.folder, compilerPath, definition);
}

function getTask(folder : string, compilerPath: string, definition?: StructuredTextBuilderTaskDefinition) :vscode.Task
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

async function getTasks(): Promise<vscode.Task[]>
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
    const compilerPath : string = "C:\\Home\\source\\Iec361131\\Projects\\OfflineCompiler\\bin\\Debug\\net5.0\\OfflineCompiler.exe";
    if(!(await exists(compilerPath))) {
        return emptyTasks;
    }

    var folder = path.dirname(editor.document.fileName)
    var buildTask = getTask(folder, compilerPath, undefined);
    return [buildTask];
}