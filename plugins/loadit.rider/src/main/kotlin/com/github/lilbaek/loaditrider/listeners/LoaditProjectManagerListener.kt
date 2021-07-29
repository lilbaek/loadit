package com.github.lilbaek.loaditrider.listeners

import com.fasterxml.jackson.databind.ObjectMapper
import com.fasterxml.jackson.module.kotlin.readValue
import com.github.lilbaek.loaditrider.model.LaunchSettings
import com.github.lilbaek.loaditrider.model.SimpleNamespaceContext
import com.intellij.openapi.diagnostic.Logger
import com.intellij.openapi.fileEditor.FileEditorManagerAdapter
import com.intellij.openapi.fileEditor.FileEditorManagerEvent
import com.intellij.openapi.fileEditor.FileEditorManagerListener
import com.intellij.openapi.project.Project
import com.intellij.openapi.project.ProjectManagerListener
import com.intellij.openapi.vfs.VirtualFileManager
import com.intellij.openapi.vfs.newvfs.BulkFileListener
import com.intellij.openapi.vfs.newvfs.events.VFileEvent
import com.intellij.util.messages.MessageBusConnection
import org.w3c.dom.Document
import org.w3c.dom.NodeList
import java.io.File
import java.io.FileInputStream
import java.io.FileWriter
import java.nio.file.Files
import java.nio.file.Path
import java.nio.file.Paths
import javax.xml.parsers.DocumentBuilderFactory
import javax.xml.transform.Transformer
import javax.xml.transform.TransformerFactory
import javax.xml.transform.dom.DOMSource
import javax.xml.transform.stream.StreamResult
import javax.xml.xpath.XPath
import javax.xml.xpath.XPathConstants
import javax.xml.xpath.XPathFactory

internal class LoaditProjectManagerListener : ProjectManagerListener {
    private val log: Logger = Logger.getInstance(
        LoaditProjectManagerListener::class.java
    )
    var launchSettings: LaunchSettings = LaunchSettings();

    override fun projectOpened(project: Project) {
        val connection: MessageBusConnection = project.messageBus.connect(project)
        if (project.basePath != null) {
            val launchfile = findFileInProject(File(project.basePath!!), "launchsettings.json")
            if (launchfile != null) {
                reloadProfilesFile(launchfile.toFile());
            }
        }
        connection.subscribe(VirtualFileManager.VFS_CHANGES, object : BulkFileListener {
            override fun after(events: List<VFileEvent?>) {
                events.forEach {
                    if (it?.file?.name != null) {
                        if (it.file!!.name.toLowerCase() == "launchsettings.json") {
                            reloadProfiles(it);
                        }
                    }
                }
            }
        })
        connection.subscribe(FileEditorManagerListener.FILE_EDITOR_MANAGER, object : FileEditorManagerAdapter() {
            override fun selectionChanged(event: FileEditorManagerEvent) {
                val editor = event.newEditor
                if (editor != null && editor.file?.isInLocalFileSystem == true && project.basePath != null) {
                    var setActive: String? = null;
                    launchSettings.profiles.forEach {
                        if (it.key.equals(editor.file!!.name, ignoreCase = true)) {
                            setActive = it.key;
                        }
                    }
                    if (setActive != null) {
                        val userFile: Path? = findFileInProject(File(project.basePath!!), ".csproj.user")
                        if (userFile != null) {
                            try {
                                val fileIS = FileInputStream(userFile.toFile())
                                val builderFactory = DocumentBuilderFactory.newInstance()
                                val builder = builderFactory.newDocumentBuilder()
                                val xmlDocument: Document = builder.parse(fileIS)
                                fileIS.close()
                                val xPath: XPath = XPathFactory.newInstance().newXPath()
                                xPath.namespaceContext = SimpleNamespaceContext(
                                    "ns",
                                    "http://schemas.microsoft.com/developer/msbuild/2003"
                                )
                                val nodeList = xPath.compile("//ActiveDebugProfile").evaluate(xmlDocument, XPathConstants.NODESET) as NodeList
                                if(nodeList.length == 1) {
                                    val item = nodeList.item(0)
                                    val child = item.firstChild;
                                    child.nodeValue = setActive;

                                    val source = DOMSource(xmlDocument)
                                    val writer = FileWriter(userFile.toFile())
                                    val result = StreamResult(writer)
                                    val transformerFactory = TransformerFactory.newInstance()
                                    val transformer: Transformer = transformerFactory.newTransformer()
                                    transformer.transform(source, result)
                                    writer.close()
                                }
                            } catch (e: Exception) {
                                log.error(e);
                            }
                        }
                    }
                }
            }
        })
    }

    private fun findFileInProject(dir: File, file: String): Path? {
        var launchfile: Path? = null;
        Files.walk(Paths.get(dir.path)).use { walkStream ->
            walkStream.filter { p ->
                p.toFile().isFile
            }.forEach { f ->

                if (f.fileName.toString().toLowerCase().endsWith(file)) {
                    launchfile = f;
                }
            }
        }
        return launchfile
    }

    private fun reloadProfilesFile(file: File) {
        try {
            val mapper = ObjectMapper();
            launchSettings = mapper.readValue(file.inputStream())
        } catch (e: Exception) {
            log.error(e);
        }
    }

    private fun reloadProfiles(vFileEvent: VFileEvent) {
        try {
            val mapper = ObjectMapper();
            launchSettings = mapper.readValue(vFileEvent.file!!.inputStream)
        } catch (e: Exception) {
            log.error(e);
        }
    }
}
