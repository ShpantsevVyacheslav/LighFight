#import <Foundation/Foundation.h>
#import <mach/mach.h>
#import <mach/task_info.h>
#import <sys/resource.h>
// Process CPU / wall is CPU core equivalents, not percentage of the whole device.
extern "C" double LFDiagnosticsCPUSeconds(){
 struct rusage usage={};if(getrusage(RUSAGE_SELF,&usage)!=0)return -1;
 return usage.ru_utime.tv_sec+usage.ru_utime.tv_usec/1e6+usage.ru_stime.tv_sec+usage.ru_stime.tv_usec/1e6;
}
extern "C" unsigned long long LFDiagnosticsFootprintBytes(){task_vm_info_data_t info={};mach_msg_type_number_t n=TASK_VM_INFO_COUNT;if(task_info(mach_task_self(),TASK_VM_INFO,(task_info_t)&info,&n)!=KERN_SUCCESS)return 0;return info.phys_footprint;}
extern "C" int LFDiagnosticsThermalState(){if(@available(iOS 11.0,*))return (int)[NSProcessInfo processInfo].thermalState;return -1;}
extern "C" int LFDiagnosticsLowPower(){if(@available(iOS 9.0,*))return [NSProcessInfo processInfo].lowPowerModeEnabled?1:0;return -1;}
