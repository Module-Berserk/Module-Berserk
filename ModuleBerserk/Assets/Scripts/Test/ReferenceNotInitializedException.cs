using System;

// 인스펙터에 반드시 drag & drop 해줘야 하는 레퍼런스들을
// 까먹고 설정하지 않았을 때 사용하는 리마인더용 exception.
public class ReferenceNotInitializedException : Exception
{
    public ReferenceNotInitializedException(string fieldName)
        : base($"{fieldName} 필드가 초기화되지 않았습니다.\n인스펙터에서 해당 필드에 컴포넌트 레퍼런스를 제공해주십시오.")
    {}
}
