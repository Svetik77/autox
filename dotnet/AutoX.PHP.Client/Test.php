<?php
require_once 'log4php/Logger.php';
require_once 'XML/Util.php';

Logger::configure('config.xml');
ini_set('soap.wsdl_cache_enabled', "0");
date_default_timezone_set('UTC');
class WebTest extends PHPUnit_Extensions_Selenium2TestCase
{
    private $log;
    private $config;
    private $clientId;
    private $registered;

//    public function testMainProcessFake()
//    {
//        //$xml = $this->fakeReadCommand('C:\Users\jien\Documents\autox\dotnet\AutoX.PHP.Client\Commands.xml');
//        //$log = Logger::getLogger('autox.log');
//        $xml = $this->fakeReadCommand('/Users/jien.huang/Documents/Commands.xml');
//        //var_dump($xml);
//        $mainId = strval($xml->attributes()->_id);
//        $this->log->debug($mainId);
//        $items = $xml->xpath("Step");
//        foreach ($items as $item) {
//            $this->doTest($item);
//            //$this->log->debug($item);
//        }
//
//
//    }

    protected function setUp()
    {
        //read config file
        //start selenium server???
        $this->log = Logger::getLogger('autox.log');
        $this->config = parse_ini_file("autox.ini");
        $this->log->debug($this->config['BrowserType']);
        $this->log->debug($this->config['DefaultURL']);
        $this->setBrowser($this->config['BrowserType']);
        $this->setBrowser("firefox");
        $this->setBrowserUrl($this->config['DefaultURL']);
    }


    public function testMainProcess(){

        while(true){
            //TODO register, then do cycle, if break from dotest cycle, register again.
            $this->registerToHost();
            $this->requestReturnCycle();
        }
    }

    protected function tearDown()
    {
        $this->closeBrowser();
    }

    private function fakeReadCommand($xmlFile)
    {
        $xml_str = file_get_contents($xmlFile);
        $xml = new SimpleXMLElement($xml_str);
        return $xml;
    }

    private function doTest($cmd)
    {
        //get action name, if it is wait 17 sec, then wait, then return null
        $this->log->debug($cmd);

        $actionName = $this->getActionName($cmd);
        $data = $this->getData($cmd);
        $uiObjectName = $this->getUIObjectName($cmd);
        $this->log->debug("Action:" . $actionName);

        if ($actionName == "Wait" && $data==null) {
            sleep(17);
            return;
        }
        $start = time();
        $stepResult = new SimpleXMLElement("<StepResult Action='" . $actionName . "' _id='" . $this->getGuid() ."' Data='" . $data ."' UIObject='" . $uiObjectName . "' StartTime='" . date("Y-m-d H:i:s", $start) . "' />");

        switch ($actionName) {
            case "Check":
                //check a checkbox or radio
                break;
            case "Click":
                $this->click($cmd, $stepResult);
                break;
            case "Close":
                $this->closeBrowser($cmd, $stepResult);
                break;
            case "Command":
                $this->command($data, $stepResult);
                break;
            case "Enter":
                $this->enter($cmd, $data, $stepResult);
                break;
            case "SetEnv":
                $this->setEnv($cmd, $stepResult);
                break;
            case "Start":
                $this->start($cmd, $stepResult);
                break;
            case "Wait":
                wait($cmd, $stepResult);
                break;
            case "GetValue":
                //
                break;
            case "Existed":
                $this->existed($cmd,$stepResult);
                break;
            case "NotExisted":
                $this->notExisted($cmd,$stepResult);
                break;
            case "VerifyValue":
                $this->verifyValue($cmd,$stepResult);
                break;
            case "VerifyTable":
                $this->verifyTable($cmd,$stepResult);
                break;
            default:
                //this is a command we don't support, do nothing here, the default ret is for this case.
                $stepResult->addAttribute("Reason","Client does not support this action" . $actionName);
                break;

        }
        $this->snapshot($stepResult);
        $end = time();
        $duration = $end - $start;
        var_dump($end);
        var_dump($duration);
        $stepResult->addAttribute("EndTime",date("Y-m-d H:i:s", $end));
        $stepResult->addAttribute("Duration",$duration->format("%I : %S"));
        var_dump($stepResult);
        return $stepResult;
    }

    /**
     * @return string
     */
    private function getGuid()
    {
        mt_srand((double)microtime() * 10000); //optional for php 4.2.0 and up.
        $charid = strtoupper(md5(uniqid(rand(), true)));
        $hyphen = chr(45); // "-"
        $uuid = substr($charid, 0, 8) . $hyphen
            . substr($charid, 8, 4) . $hyphen
            . substr($charid, 12, 4) . $hyphen
            . substr($charid, 16, 4) . $hyphen
            . substr($charid, 20, 12);
        return strval($uuid);
    }

    private function getClientId(){
        if(empty($this->clientId))
            $this->clientId = $this->getGuid();
        return $this->clientId;
    }
    //register
    private function getRegisterString(){
        $computerName = getHostName();
        $ipAddress = gethostbyname($computerName);
        $version = PHP_OS;
        $cmd = $this->initXCommand("Register","ComputerName",$computerName,"IPAddress",$ipAddress,"Version",$version,"_id",$this->getClientId(),"DefaultURL",$this->config['DefaultURL']);
        return $cmd->asXML();
    }

    private function getRequestCommandString(){
        $computerName = getHostName();
        $cmd = $this->initXCommand("RequestCommand","ComputerName",$computerName,"_id",$this->clientId);
        return $cmd->asXML();
    }

    private function getSetResultString($result){
        $cmd = $this->initXCommand("SetResult","ClientId",$this->clientId);
        $this->xml_appendChild($cmd,$result);
        return $cmd->asXML();
    }

    private function xml_appendChild(SimpleXMLElement $to, SimpleXMLElement $from) {
        $toDom = dom_import_simplexml($to);
        $fromDom = dom_import_simplexml($from);
        $toDom->appendChild($toDom->ownerDocument->importNode($fromDom, true));
    }

    private function initXCommand(){
        $numargs = func_num_args();
        $action = func_get_arg(0);
        $cmd = new SimpleXMLElement("<Command />");
        $cmd->addAttribute("Action",$action);
        $arg_list = func_get_args();
        for($i=1;$i<$numargs;$i++){
            $key = $arg_list[$i];
            $i++;
            $value = $arg_list[$i];
            $cmd->addAttribute($key,$value);
        }

        return $cmd;
    }

    //read command from center, return an xml

    private function getActionName($cmd)
    {
        return strval($cmd["Action"]);
    }

    //run the command, return an xml format string

    private function getData($cmd)
    {
        return strval($cmd["Data"]);
    }

    private function getUIObjectName($cmd){
        return strval($cmd["UIObject"]);
    }

    private function getInstanceId($cmd){
        return strval($cmd["InstanceId"]);
    }


    private function getUIObject($cmd)
    {
        $uiobject = $cmd->xpath("UIObject");
        $xpath = $uiobject[0]["XPath"];
        return strval($xpath);
    }

    private function setSuccessResult($stepResult){
        $stepResult->addAttribute("Result","Success");
    }

    private function setFailedResult($stepResult,$reason){
        $stepResult->addAttribute("Result","Error");
        if(!empty($reason)){
            $stepResult->addAttribute("Reason",$reason);
        }
    }

//----------------actions-----------------
    private function click($xmlElement, $stepResult)
    {
        $xpath = $this->getUIObject($xmlElement);
        $this->byXPath($xpath)->click();

        $this->setSuccessResult($stepResult);
    }

    private function closeBrowser($xmlElemen, $stepResultt)
    {
        $this->webdriver->close();
        $this->setSuccessResult($stepResult);
    }

    private function command($cmd, $stepResult)
    {
        exec($cmd);
        $this->setSuccessResult($stepResult);
    }

    //send result back to center, xml format string

    private function enter($xmlElement, $data, $stepResult)
    {
        $xpath = $this->getUIObject($xmlElement);
        $this->byXPath($xpath)->click();
        $this->keys($data);
        $this->setSuccessResult($stepResult);
    }

    private function setEnv($xmlElement, $stepResult)
    {
        foreach ($xmlElement->attributes() as $key => $value) {
            $this->config[$key] = strval($value);
        }
        $this->setBrowser($this->config['BrowserType']);
        $this->setBrowserUrl($this->config['DefaultURL']);
        $this->prepareSession();
        $this->log->debug($this->config['DefaultURL']);
        $this->url($this->config['DefaultURL']);
        $this->setSuccessResult($stepResult);
    }

    private function start($xmlElement, $stepResult)
    {
        $this->setBrowser($this->config['BrowserType']);
        $this->setBrowserUrl($this->config['DefaultURL']);
        $this->prepareSession();
        $this->url($this->config['DefaultURL']);
        $this->setSuccessResult($stepResult);
    }

    private function existed($xmlElement, $stepResult)
    {
        $xpath = $this->getUIObject($xmlElement);
        //TODO how to set result????

        $this->setSuccessResult($stepResult);
    }

    private function notExisted($xmlElement, $stepResult)
    {
        $xpath = $this->getUIObject($xmlElement);
        //TODO how to set result????
        $this->setSuccessResult($stepResult);
    }
    private function verifyValue($xmlElement, $stepResult)
    {
        $xpath = $this->getUIObject($xmlElement);
        //TODO how to set result????
        $this->setSuccessResult($stepResult);
    }
    private function verifyTable($xmlElement, $stepResult)
    {
        $xpath = $this->getUIObject($xmlElement);
        //TODO how to set result????
        $this->setSuccessResult($stepResult);
    }

    private function wait($xmlElement, $stepResult)
    {
        $time = 17;
        try {
            $data = $xmlElement['Data'];
            $time = intval($data);
        } catch (Exception $e) {
        }
        sleep($time);
        $this->setSuccessResult($stepResult);
    }
//------------actions---------------------

    private function snapshot($stepResult)
    {
        //TODO read the related config, do screen shot when required; below function return base64 string
        if(strval($stepResult["Result"])!="Success")
            $stepResult->addAttribute("Link", $this->screenshot());
    }

    private function readCommand($cmd)
    {
        //$this->log->debug($this->config["EndPoint"]);
        $this->log->debug($cmd);
        try {
            $soap = new SoapClient($this->config["EndPoint"]);
            $param["xmlFormatCommand"] = $cmd;
            $ret = $soap->__Call("Command", array($param));
            var_dump($ret);
            $xml_str = $ret->CommandResult;
            $this->log->debug($xml_str);
            return new SimpleXMLElement($xml_str);
        } catch (Exception $e) {
            echo print_r($e->getMessage(), true);
            $this->log->error($e->getMessage());
        }
        return null;
    }

    private function registerToHost(){
        $cmd = $this->getRegisterString();

        while(1){
            try{
                $result = $this->readCommand($cmd);
                if($result!=null)
                    return;
            }catch(Exception $e){
                $this->log->warn("Register failed, due to: "+$e->getMessage());
                sleep(17);
            }
        }
    }

    private function requestCommandFromHost(){
        $cmd = $this->getRequestCommandString();
        try{
            $command = $this->readCommand($cmd);
            var_dump($command);
            if($command!=null)
                return $command;
        }catch(Exception $e){
            $this->log->error("Request command failed, due to: "+$e->getMessage());
            sleep(6);
        }
        return null;
    }
    private function sendResultToHost($ret)
    {
        $cmd = $this->getSetResultString($ret);
        $this->readCommand($cmd);
    }


    private function requestReturnCycle()
    {
        while (true) {
            //read command from center

            $command = $this->requestCommandFromHost();
            if (empty($command)) {
                sleep(6);
                continue;
            }

            $instanceId = $this->getInstanceId($command);
            if (empty($instanceId)) {
                sleep(6);
                continue;
            }
            //TODO prepare the return result here
            $result = new SimpleXMLElement("<Result />");
            $result->addAttribute("Created", date("Y-m-d H:i:s", time()));
            //$result->addAttribute("_id",$instanceId);
            $result->addAttribute("InstanceId",$instanceId);
            //analasyze the xml, choose a action to run
            $items = $command->xpath("Step");
            foreach ($items as $item) {
                $this->log->debug($item);
                $ret = $this->doTest($item);
                $this->xml_appendChild($result, $ret);
            }
            $result->addAttribute("Updated", date("Y-m-d H:i:s", time()));
            $this->sendResultToHost($result);
        }
    }



}

?>